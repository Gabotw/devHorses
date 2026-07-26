import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { AutoCompleteModule, AutoCompleteCompleteEvent } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { CheckInsService } from '../../core/checkins.service';
import { MembersService } from '../../core/members.service';
import { CheckIn, CheckInResult, Member } from '../../core/models';

@Component({
  selector: 'app-checkin',
  imports: [FormsModule, DatePipe, AutoCompleteModule, ButtonModule, TableModule, TagModule],
  templateUrl: './checkin.html',
  styleUrl: './checkin.scss',
})
export class CheckInPage implements OnInit, OnDestroy {
  private readonly checkinsApi = inject(CheckInsService);
  private readonly membersApi = inject(MembersService);
  private readonly messages = inject(MessageService);

  readonly occupancy = signal(0);
  readonly today = signal<CheckIn[]>([]);
  readonly suggestions = signal<Member[]>([]);
  readonly lastResult = signal<CheckInResult | null>(null);
  readonly registering = signal(false);
  selected: Member | string | null = null;

  ngOnInit(): void {
    this.loadOccupancy();
    this.loadToday();
    this.checkinsApi
      .connectOccupancy((value) => this.occupancy.set(value))
      .catch(() => {
        // El aforo en vivo es un extra; si el hub no conecta, seguimos con el conteo por HTTP.
        this.messages.add({ severity: 'warn', summary: 'Aforo en vivo no disponible' });
      });
  }

  ngOnDestroy(): void {
    void this.checkinsApi.disconnectOccupancy();
  }

  searchMembers(event: AutoCompleteCompleteEvent): void {
    this.membersApi.list(event.query, 1, 8).subscribe((res) => this.suggestions.set(res.items));
  }

  register(): void {
    const member = this.selected;
    if (!member || typeof member === 'string' || !member.id) {
      this.messages.add({ severity: 'warn', summary: 'Elige un miembro de la lista' });
      return;
    }

    this.registering.set(true);
    this.checkinsApi.register(member.id).subscribe({
      next: (result) => {
        this.registering.set(false);
        this.lastResult.set(result);
        this.occupancy.set(result.occupancy);
        this.selected = null;
        this.suggestions.set([]);
        if (result.checkIn.isValid) {
          this.messages.add({ severity: 'success', summary: `Ingreso de ${result.checkIn.memberName}` });
        } else {
          this.messages.add({ severity: 'error', summary: 'Ingreso no válido', detail: result.checkIn.reason ?? '' });
        }
        this.loadToday();
      },
      error: (err) => {
        this.registering.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo registrar el ingreso' });
      },
    });
  }

  private loadOccupancy(): void {
    this.checkinsApi.occupancy().subscribe((r) => this.occupancy.set(r.occupancy));
  }

  private loadToday(): void {
    this.checkinsApi.today().subscribe((list) => this.today.set(list));
  }
}
