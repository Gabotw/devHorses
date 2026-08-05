import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { AuthService } from '../../core/auth.service';
import { MemberPayload, MembersService } from '../../core/members.service';
import { MembershipsService } from '../../core/memberships.service';
import { PaymentsService } from '../../core/payments.service';
import { PlansService } from '../../core/plans.service';
import {
  MEMBER_STATUS_LABEL,
  MEMBERSHIP_STATUS_LABEL,
  PAYMENT_METHOD_LABEL,
  PAYMENT_STATUS_LABEL,
  Member,
  Membership,
  Payment,
  Plan,
} from '../../core/models';

@Component({
  selector: 'app-members-list',
  imports: [
    FormsModule, DecimalPipe, DatePipe, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, SelectModule, DatePickerModule, TagModule, TooltipModule,
  ],
  templateUrl: './members-list.html',
  styleUrl: './members-list.scss',
})
export class MembersList {
  private readonly membersApi = inject(MembersService);
  private readonly plansApi = inject(PlansService);
  private readonly membershipsApi = inject(MembershipsService);
  private readonly paymentsApi = inject(PaymentsService);
  private readonly messages = inject(MessageService);
  readonly auth = inject(AuthService);

  readonly members = signal<Member[]>([]);
  readonly loading = signal(false);
  readonly total = signal(0);
  search = '';
  page = 1;
  readonly pageSize = 20;

  // Diálogo alta/edición
  readonly editing = signal<Member | null>(null);
  readonly showForm = signal(false);
  form: MemberPayload = this.emptyForm();
  readonly saving = signal(false);

  // Diálogo de membresías
  readonly showMemberships = signal(false);
  readonly selectedMember = signal<Member | null>(null);
  readonly history = signal<Membership[]>([]);
  readonly activePlans = signal<Plan[]>([]);
  planToAssign: string | null = null;
  assignStart: Date | null = null;

  // Pagos (Fase 2)
  readonly payments = signal<Payment[]>([]);
  payAmount: number | null = null;
  payNotes = '';
  readonly registeringPayment = signal(false);

  readonly statusLabel = MEMBER_STATUS_LABEL;
  readonly membershipStatusLabel = MEMBERSHIP_STATUS_LABEL;
  readonly paymentMethodLabel = PAYMENT_METHOD_LABEL;
  readonly paymentStatusLabel = PAYMENT_STATUS_LABEL;

  readonly currentMembership = computed(() =>
    this.history().find((m) => m.status === 1 || m.status === 2) ?? null,
  );

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.membersApi.list(this.search, this.page, this.pageSize).subscribe({
      next: (res) => {
        this.members.set(res.items);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: 'No se pudo cargar miembros' });
      },
    });
  }

  onSearch(): void {
    this.page = 1;
    this.load();
  }

  onPage(event: TableLazyLoadEvent): void {
    const first = event.first ?? 0;
    const rows = event.rows ?? this.pageSize;
    this.page = Math.floor(first / rows) + 1;
    this.load();
  }

  // ---- Alta / edición ----

  openNew(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.showForm.set(true);
  }

  openEdit(m: Member): void {
    this.editing.set(m);
    this.form = {
      fullName: m.fullName,
      documentId: m.documentId,
      phone: m.phone ?? '',
      email: m.email ?? '',
      photoUrl: m.photoUrl ?? '',
    };
    this.showForm.set(true);
  }

  saveMember(): void {
    if (!this.form.fullName?.trim() || !this.form.documentId?.trim()) {
      this.messages.add({ severity: 'warn', summary: 'Nombre y documento son obligatorios' });
      return;
    }
    this.saving.set(true);
    const editing = this.editing();
    const op = editing
      ? this.membersApi.update(editing.id, this.form)
      : this.membersApi.create(this.form);

    op.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.messages.add({ severity: 'success', summary: editing ? 'Miembro actualizado' : 'Miembro creado' });
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo guardar' });
      },
    });
  }

  regenerateCode(m: Member): void {
    this.membersApi.regenerateCode(m.id).subscribe({
      next: (updated) => {
        this.messages.add({ severity: 'success', summary: `Código de ${updated.fullName}: ${updated.accessCode}` });
        this.load();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo generar el código' }),
    });
  }

  toggleActive(m: Member): void {
    const op = m.status === 1 ? this.membersApi.deactivate(m.id) : this.membersApi.activate(m.id);
    op.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: m.status === 1 ? 'Miembro desactivado' : 'Miembro activado' });
        this.load();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  // ---- Membresías ----

  openMemberships(m: Member): void {
    this.selectedMember.set(m);
    this.planToAssign = null;
    this.assignStart = null;
    this.payAmount = null;
    this.payNotes = '';
    this.payments.set([]);
    this.showMemberships.set(true);
    this.loadHistory(m.id);
    this.loadPayments(m.id);
    this.plansApi.list(false).subscribe((p) => this.activePlans.set(p));
  }

  private loadHistory(memberId: string): void {
    this.membersApi.memberships(memberId).subscribe((h) => this.history.set(h));
  }

  private loadPayments(memberId: string): void {
    this.paymentsApi.listByMember(memberId).subscribe((p) => this.payments.set(p));
  }

  assignPlan(): void {
    const member = this.selectedMember();
    if (!member || !this.planToAssign) {
      this.messages.add({ severity: 'warn', summary: 'Elige un plan' });
      return;
    }
    const start = this.assignStart ? this.toIsoDate(this.assignStart) : undefined;
    this.membershipsApi.create(member.id, this.planToAssign, start).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: 'Membresía asignada' });
        this.planToAssign = null;
        this.assignStart = null;
        this.loadHistory(member.id);
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  freeze(m: Membership): void {
    const from = new Date();
    const until = new Date();
    until.setDate(until.getDate() + 7); // congelamiento sugerido de 1 semana
    this.membershipsApi.freeze(m.id, this.toIsoDate(from), this.toIsoDate(until)).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: 'Membresía congelada 7 días' });
        this.reloadHistory();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  unfreeze(m: Membership): void {
    this.membershipsApi.unfreeze(m.id, this.toIsoDate(new Date())).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: 'Membresía reactivada' });
        this.reloadHistory();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  private reloadHistory(): void {
    const m = this.selectedMember();
    if (m) this.loadHistory(m.id);
  }

  // ---- Pagos (Fase 2) ----

  registerPayment(): void {
    const member = this.selectedMember();
    if (!member) return;
    if (!this.payAmount || this.payAmount <= 0) {
      this.messages.add({ severity: 'warn', summary: 'Ingresa un monto mayor a cero' });
      return;
    }

    this.registeringPayment.set(true);
    this.paymentsApi.registerCash({
      memberId: member.id,
      membershipId: this.currentMembership()?.id ?? null,
      amount: this.payAmount,
      notes: this.payNotes?.trim() || null,
    }).subscribe({
      next: () => {
        this.registeringPayment.set(false);
        this.payAmount = null;
        this.payNotes = '';
        this.messages.add({ severity: 'success', summary: 'Pago registrado' });
        this.loadPayments(member.id);
      },
      error: (err) => {
        this.registeringPayment.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo registrar el pago' });
      },
    });
  }

  statusSeverity(status: number): 'success' | 'warn' | 'danger' | 'info' {
    switch (status) {
      case 1: return 'success';
      case 2: return 'info';
      case 3: return 'danger';
      default: return 'warn';
    }
  }

  paymentSeverity(status: number): 'success' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 2: return 'success';  // Completado
      case 1: return 'warn';     // Pendiente
      case 3: return 'danger';   // Fallido
      default: return 'secondary';
    }
  }

  private toIsoDate(d: Date): string {
    // yyyy-MM-dd sin desfase de zona.
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private emptyForm(): MemberPayload {
    return { fullName: '', documentId: '', phone: '', email: '', photoUrl: '' };
  }
}
