import { DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';

import { ReportsService } from '../../core/reports.service';
import {
  Dashboard,
  MEMBERSHIP_STATUS_LABEL,
  PAYMENT_METHOD_LABEL,
} from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [
    FormsModule,
    DatePipe,
    DecimalPipe,
    PercentPipe,
    ButtonModule,
    DatePickerModule,
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class DashboardPage implements OnInit {
  private readonly reportsApi = inject(ReportsService);
  private readonly messages = inject(MessageService);

  readonly data = signal<Dashboard | null>(null);
  readonly loading = signal(false);

  // Rango: por defecto los últimos 30 días (el backend usa el mismo default si no se envía).
  range: Date[] = [daysAgo(29), new Date()];

  readonly statusLabel = MEMBERSHIP_STATUS_LABEL;
  readonly methodLabel = PAYMENT_METHOD_LABEL;

  /** Monto diario máximo del rango, para escalar las barras de ingresos. */
  readonly maxRevenue = computed(() =>
    Math.max(1, ...(this.data()?.revenueByDay.map((p) => p.amount) ?? [0])),
  );

  /** Ingresos máximos por hora, para escalar las barras de ocupación. */
  readonly maxOccupancy = computed(() =>
    Math.max(1, ...(this.data()?.occupancyByHour.map((p) => p.count) ?? [0])),
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const [from, to] = this.range ?? [];
    this.loading.set(true);
    this.reportsApi.dashboard(toIsoDate(from), toIsoDate(to)).subscribe({
      next: (d) => {
        this.data.set(d);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.messages.add({
          severity: 'error',
          summary: 'No se pudo cargar el dashboard',
          detail: err.error?.detail ?? '',
        });
      },
    });
  }

  barHeight(amount: number, max: number): number {
    return Math.round((amount / max) * 100);
  }
}

function daysAgo(n: number): Date {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d;
}

/** Fecha local → yyyy-MM-dd (sin desfase de zona por usar toISOString). */
function toIsoDate(d: Date | null | undefined): string | undefined {
  if (!d) return undefined;
  const y = d.getFullYear();
  const m = `${d.getMonth() + 1}`.padStart(2, '0');
  const day = `${d.getDate()}`.padStart(2, '0');
  return `${y}-${m}-${day}`;
}
