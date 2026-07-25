import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { AuthService } from '../../core/auth.service';
import { PlanPayload, PlansService } from '../../core/plans.service';
import { Plan } from '../../core/models';

@Component({
  selector: 'app-plans-list',
  imports: [
    FormsModule, DecimalPipe, TableModule, ButtonModule, DialogModule,
    InputTextModule, InputNumberModule, TagModule,
  ],
  templateUrl: './plans-list.html',
})
export class PlansList {
  private readonly plansApi = inject(PlansService);
  private readonly messages = inject(MessageService);
  readonly auth = inject(AuthService);

  readonly plans = signal<Plan[]>([]);
  readonly loading = signal(false);
  includeInactive = false;

  readonly showForm = signal(false);
  readonly editing = signal<Plan | null>(null);
  readonly saving = signal(false);
  form: PlanPayload = this.emptyForm();

  constructor() {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.plansApi.list(this.includeInactive).subscribe({
      next: (p) => {
        this.plans.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messages.add({ severity: 'error', summary: 'No se pudo cargar planes' });
      },
    });
  }

  openNew(): void {
    this.editing.set(null);
    this.form = this.emptyForm();
    this.showForm.set(true);
  }

  openEdit(p: Plan): void {
    this.editing.set(p);
    this.form = {
      name: p.name,
      price: p.price,
      durationDays: p.durationDays,
      monthlyAccesses: p.monthlyAccesses ?? null,
    };
    this.showForm.set(true);
  }

  save(): void {
    if (!this.form.name?.trim() || this.form.durationDays < 1 || this.form.price < 0) {
      this.messages.add({ severity: 'warn', summary: 'Revisa nombre, precio y duración' });
      return;
    }
    this.saving.set(true);
    const editing = this.editing();
    const op = editing ? this.plansApi.update(editing.id, this.form) : this.plansApi.create(this.form);
    op.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.messages.add({ severity: 'success', summary: editing ? 'Plan actualizado' : 'Plan creado' });
        this.load();
      },
      error: (err) => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail ?? 'No se pudo guardar' });
      },
    });
  }

  toggleActive(p: Plan): void {
    const op = p.isActive ? this.plansApi.deactivate(p.id) : this.plansApi.activate(p.id);
    op.subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: p.isActive ? 'Plan desactivado' : 'Plan activado' });
        this.load();
      },
      error: (err) => this.messages.add({ severity: 'error', summary: 'Error', detail: err.error?.detail }),
    });
  }

  private emptyForm(): PlanPayload {
    return { name: '', price: 0, durationDays: 30, monthlyAccesses: null };
  }
}
