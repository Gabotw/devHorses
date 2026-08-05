import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { MembershipsService } from '../../core/memberships.service';
import { ExpiringMembership, MEMBERSHIP_STATUS_LABEL } from '../../core/models';

@Component({
  selector: 'app-expirations',
  imports: [FormsModule, TableModule, ButtonModule, TagModule, SelectModule],
  templateUrl: './expirations.html',
  styleUrl: './expirations.scss',
})
export class ExpirationsPage {
  private readonly membershipsApi = inject(MembershipsService);
  private readonly messages = inject(MessageService);

  readonly items = signal<ExpiringMembership[]>([]);
  readonly loading = signal(false);
  withinDays = 7;

  readonly rangeOptions = [
    { label: 'Próximos 3 días', value: 3 },
    { label: 'Próximos 7 días', value: 7 },
    { label: 'Próximos 15 días', value: 15 },
    { label: 'Próximos 30 días', value: 30 },
  ];

  readonly statusLabel = MEMBERSHIP_STATUS_LABEL;

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.membershipsApi.expiring(this.withinDays).subscribe({
      next: (list) => {
        this.items.set(list);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: 'No se pudo cargar los vencimientos' });
      },
    });
  }

  daysLabel(d: number): string {
    if (d < 0) return `Venció hace ${-d} día${-d === 1 ? '' : 's'}`;
    if (d === 0) return 'Vence hoy';
    return `En ${d} día${d === 1 ? '' : 's'}`;
  }

  daysSeverity(d: number): 'danger' | 'warn' | 'info' {
    if (d <= 0) return 'danger';
    if (d <= 3) return 'warn';
    return 'info';
  }

  /** Abre WhatsApp con un mensaje prellenado para que la recepción lo envíe. */
  notify(row: ExpiringMembership): void {
    const phone = this.normalizePhone(row.phone);
    if (!phone) {
      this.messages.add({ severity: 'warn', summary: 'Sin teléfono', detail: `${row.fullName} no tiene teléfono registrado` });
      return;
    }
    const url = `https://wa.me/${phone}?text=${encodeURIComponent(this.buildMessage(row))}`;
    window.open(url, '_blank', 'noopener');
  }

  private buildMessage(row: ExpiringMembership): string {
    const venceTxt = row.daysToExpiry < 0
      ? `venció el ${row.endDate}`
      : row.daysToExpiry === 0
        ? `vence hoy (${row.endDate})`
        : `vence el ${row.endDate} (en ${row.daysToExpiry} días)`;
    return `Hola ${row.fullName} 👋\n` +
      `Te recordamos que tu membresía (${row.planName}) ${venceTxt}.\n` +
      `Acércate a recepción para renovarla. ¡Gracias!`;
  }

  /** Deja solo dígitos y antepone 51 (Perú) si parece un móvil local de 9 dígitos. */
  private normalizePhone(phone?: string | null): string | null {
    if (!phone) return null;
    const digits = phone.replace(/\D/g, '');
    if (!digits) return null;
    return digits.length === 9 ? `51${digits}` : digits;
  }
}
