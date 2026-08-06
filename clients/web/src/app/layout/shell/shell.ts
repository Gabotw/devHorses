import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule],
  styleUrl: './shell.scss',
  template: `
    <header class="topbar">
      <div class="brand">
        <i class="pi pi-bolt"></i>
        <span>GymFlow</span>
      </div>

      <nav class="nav">
        @if (auth.isManager()) {
          <a routerLink="/dashboard" routerLinkActive="active">
            <i class="pi pi-chart-bar"></i> Dashboard
          </a>
        }
        <a routerLink="/checkin" routerLinkActive="active">
          <i class="pi pi-sign-in"></i> Check-in
        </a>
        <a routerLink="/members" routerLinkActive="active">
          <i class="pi pi-users"></i> Miembros
        </a>
        <a routerLink="/expirations" routerLinkActive="active">
          <i class="pi pi-clock"></i> Vencimientos
        </a>
        <a routerLink="/plans" routerLinkActive="active">
          <i class="pi pi-tags"></i> Planes
        </a>
        @if (auth.isManager()) {
          <a routerLink="/staff" routerLinkActive="active">
            <i class="pi pi-user-edit"></i> Personal
          </a>
        }
      </nav>

      <div class="user">
        <span class="who">
          {{ auth.currentUser()?.fullName }}
          <small>{{ auth.role() }}</small>
        </span>
        <p-button icon="pi pi-sign-out" severity="secondary" [text]="true"
          (onClick)="logout()" ariaLabel="Salir" />
      </div>
    </header>

    <main>
      <router-outlet />
    </main>
  `,
})
export class Shell {
  readonly auth = inject(AuthService);

  logout(): void {
    this.auth.logout();
    location.assign('/login');
  }
}
