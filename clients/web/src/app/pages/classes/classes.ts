import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { ClassesService } from '../../core/classes.service';
import {
  CLASS_RESERVATION_STATUS_LABEL,
  ClassReservation,
  ClassSession,
} from '../../core/models';

interface ClassForm {
  name: string;
  instructorName: string;
  startsAt: Date | null;
  durationMinutes: number | null;
  capacity: number | null;
}

@Component({
  selector: 'app-classes',
  imports: [
    FormsModule, DatePipe, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, DatePickerModule, TagModule,
  ],
  templateUrl: './classes.html',
  styleUrl: './classes.scss',
})
export class ClassesPage {
  private readonly classesApi = inject(ClassesService);
  private readonly messages = inject(MessageService);

  readonly sessions = signal<ClassSession[]>([]);
  readonly loading = signal(false);

  // Diálogo nueva clase
  readonly showForm = signal(false);
  readonly saving = signal(false);
  form: ClassForm = this.emptyForm();

  // Diálogo roster
  readonly showRoster = signal(false);
  readonly selected = signal<ClassSession | null>(null);
  readonly roster = signal<ClassReservation[]>([]);

  readonly reservationLabel = CLASS_RESERVATION_STATUS_LABEL;

  // Puentes para el [(visible)] de p-dialog (los signals no soportan banana-in-a-box directo).
  get showFormValue(): boolean { return this.showForm(); }
  set showFormValue(v: boolean) { this.showForm.set(v); }
  get showRosterValue(): boolean { return this.showRoster(); }
  set showRosterValue(v: boolean) { this.showRoster.set(v); }

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.classesApi.list().subscribe({
      next: (list) => {
        this.sessions.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: 'No se pudo cargar las clases' });
      },
    });
  }

  openNew(): void {
    this.form = this.emptyForm();
    this.showForm.set(true);
  }

  save(): void {
    const f = this.form;
    if (!f.name?.trim() || !f.startsAt || !f.durationMinutes || !f.capacity) {
      this.messages.add({ severity: 'warn', summary: 'Completa nombre, fecha, duración y cupo' });
      return;
    }
    this.saving.set(true);
    this.classesApi.create({
      name: f.name.trim(),
      instructorName: f.instructorName?.trim() || null,
      startsAtUtc: f.startsAt.toISOString(),
      durationMinutes: f.durationMinutes,
      capacity: f.capacity,
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.messages.add({ severity: 'success', summary: 'Clase creada' });
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo crear la clase' });
      },
    });
  }

  cancelSession(s: ClassSession): void {
    this.classesApi.cancel(s.id).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: 'Clase cancelada' });
        this.load();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  openRoster(s: ClassSession): void {
    this.selected.set(s);
    this.roster.set([]);
    this.showRoster.set(true);
    this.classesApi.roster(s.id).subscribe((r) => this.roster.set(r));
  }

  markAttendance(r: ClassReservation): void {
    this.classesApi.markAttendance(r.classSessionId, r.memberId).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: `Asistencia de ${r.memberName}` });
        const s = this.selected();
        if (s) this.classesApi.roster(s.id).subscribe((list) => this.roster.set(list));
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  reservationSeverity(status: number): 'success' | 'warn' | 'danger' | 'info' {
    switch (status) {
      case 1: return 'success';  // Con cupo
      case 2: return 'warn';     // En espera
      case 4: return 'info';     // Asistió
      default: return 'danger';  // Cancelada
    }
  }

  private emptyForm(): ClassForm {
    return { name: '', instructorName: '', startsAt: null, durationMinutes: 60, capacity: 15 };
  }
}
