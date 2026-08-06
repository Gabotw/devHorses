import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { CreateStaffPayload, StaffService } from '../../core/staff.service';
import { STAFF_ROLE_LABEL, StaffRole, StaffUser } from '../../core/models';

@Component({
  selector: 'app-staff',
  imports: [
    FormsModule, DatePipe, TableModule, ButtonModule, DialogModule,
    InputTextModule, PasswordModule, SelectModule, TagModule, TooltipModule,
  ],
  templateUrl: './staff.html',
  styleUrl: './staff.scss',
})
export class StaffPage {
  private readonly staffApi = inject(StaffService);
  private readonly messages = inject(MessageService);

  readonly users = signal<StaffUser[]>([]);
  readonly loading = signal(false);

  readonly roleLabel = STAFF_ROLE_LABEL;
  readonly roleOptions = [
    { label: 'Administrador', value: 2 as StaffRole },
    { label: 'Recepción', value: 3 as StaffRole },
    { label: 'Dueño', value: 1 as StaffRole },
  ];

  // Alta / edición
  readonly editing = signal<StaffUser | null>(null);
  readonly showForm = signal(false);
  readonly saving = signal(false);
  form: CreateStaffPayload = this.emptyForm();

  // Reset de contraseña
  readonly showReset = signal(false);
  readonly resetting = signal(false);
  resetTarget: StaffUser | null = null;
  newPassword = '';

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.staffApi.list().subscribe({
      next: (list) => {
        this.users.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: 'No se pudo cargar el personal' });
      },
    });
  }

  openNew(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.showForm.set(true);
  }

  openEdit(u: StaffUser): void {
    this.editing.set(u);
    this.form = { fullName: u.fullName, email: u.email, password: '', role: u.role };
    this.showForm.set(true);
  }

  save(): void {
    if (!this.form.fullName?.trim()) {
      this.messages.add({ severity: 'warn', summary: 'El nombre es obligatorio' });
      return;
    }
    const editing = this.editing();
    if (!editing) {
      if (!this.form.email?.trim()) {
        this.messages.add({ severity: 'warn', summary: 'El correo es obligatorio' });
        return;
      }
      if (!this.form.password || this.form.password.length < 6) {
        this.messages.add({ severity: 'warn', summary: 'La contraseña debe tener al menos 6 caracteres' });
        return;
      }
    }

    this.saving.set(true);
    const op = editing
      ? this.staffApi.update(editing.id, { fullName: this.form.fullName, role: this.form.role })
      : this.staffApi.create(this.form);

    op.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.messages.add({ severity: 'success', summary: editing ? 'Usuario actualizado' : 'Usuario creado' });
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo guardar' });
      },
    });
  }

  toggleActive(u: StaffUser): void {
    const op = u.isActive ? this.staffApi.deactivate(u.id) : this.staffApi.activate(u.id);
    op.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: u.isActive ? 'Usuario desactivado' : 'Usuario activado' });
        this.load();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  openReset(u: StaffUser): void {
    this.resetTarget = u;
    this.newPassword = '';
    this.showReset.set(true);
  }

  confirmReset(): void {
    const target = this.resetTarget;
    if (!target) return;
    if (!this.newPassword || this.newPassword.length < 6) {
      this.messages.add({ severity: 'warn', summary: 'La contraseña debe tener al menos 6 caracteres' });
      return;
    }
    this.resetting.set(true);
    this.staffApi.resetPassword(target.id, this.newPassword).subscribe({
      next: () => {
        this.resetting.set(false);
        this.showReset.set(false);
        this.messages.add({ severity: 'success', summary: `Contraseña actualizada para ${target.fullName}` });
      },
      error: (err) => {
        this.resetting.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail });
      },
    });
  }

  roleSeverity(role: number): 'info' | 'success' | 'secondary' {
    switch (role) {
      case 1: return 'success';   // Dueño
      case 2: return 'info';      // Admin
      default: return 'secondary'; // Recepción
    }
  }

  private emptyForm(): CreateStaffPayload {
    return { fullName: '', email: '', password: '', role: 3 };
  }
}
