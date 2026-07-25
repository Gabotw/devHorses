import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, ButtonModule, InputTextModule, PasswordModule],
  styleUrl: './login.scss',
  template: `
    <div class="login-wrap">
      <form class="card" (ngSubmit)="submit()">
        <div class="logo">
          <i class="pi pi-bolt"></i>
          <h1>GymFlow</h1>
        </div>
        <p class="sub">Panel de administración</p>

        <div class="field">
          <label for="sub">Gimnasio</label>
          <input pInputText id="sub" name="sub" [(ngModel)]="subdomain"
            placeholder="demo" autocomplete="organization" required />
        </div>

        <div class="field">
          <label for="email">Correo</label>
          <input pInputText id="email" name="email" type="email" [(ngModel)]="email"
            placeholder="owner@demo.gymflow.pe" autocomplete="username" required />
        </div>

        <div class="field">
          <label for="pass">Contraseña</label>
          <p-password id="pass" name="pass" [(ngModel)]="password" [feedback]="false"
            [toggleMask]="true" inputStyleClass="w-full" styleClass="w-full" required />
        </div>

        <p-button type="submit" label="Ingresar" [loading]="loading()"
          styleClass="w-full" icon="pi pi-sign-in" />
      </form>
    </div>
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly messages = inject(MessageService);

  subdomain = 'demo';
  email = '';
  password = '';
  readonly loading = signal(false);

  submit(): void {
    if (!this.subdomain || !this.email || !this.password) {
      this.messages.add({ severity: 'warn', summary: 'Completa todos los campos' });
      return;
    }

    this.loading.set(true);
    this.auth.login(this.subdomain, this.email, this.password).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/members']);
      },
      error: (err) => {
        this.loading.set(false);
        const detail = err.status === 401
          ? 'Credenciales inválidas.'
          : err.error?.error ?? 'No se pudo iniciar sesión.';
        this.messages.add({ severity: 'error', summary: 'Error', detail });
      },
    });
  }
}
