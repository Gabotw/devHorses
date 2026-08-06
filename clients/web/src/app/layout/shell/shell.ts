import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule],
  styleUrl: './shell.scss',
  template: `
    <div class="layout">
      <aside class="sidebar">
        <div class="brand">
          <span class="brand-mark"><i class="pi pi-bolt"></i></span>
          <span class="brand-name">GymFlow</span>
        </div>

        <nav class="nav">
          @if (auth.isManager()) {
            <a routerLink="/dashboard" routerLinkActive="active">
              <i class="pi pi-chart-bar"></i> <span>Dashboard</span>
            </a>
          }
          <a routerLink="/checkin" routerLinkActive="active">
            <i class="pi pi-sign-in"></i> <span>Check-in</span>
          </a>
          <a routerLink="/members" routerLinkActive="active">
            <i class="pi pi-users"></i> <span>Miembros</span>
          </a>
          <a routerLink="/expirations" routerLinkActive="active">
            <i class="pi pi-clock"></i> <span>Vencimientos</span>
          </a>
          <a routerLink="/plans" routerLinkActive="active">
            <i class="pi pi-tags"></i> <span>Planes</span>
          </a>
          @if (auth.isManager()) {
            <a routerLink="/staff" routerLinkActive="active">
              <i class="pi pi-user-edit"></i> <span>Personal</span>
            </a>
          }
        </nav>

        <div class="user">
          <div class="avatar">{{ initials() }}</div>
          <div class="who">
            <span class="name">{{ auth.currentUser()?.fullName }}</span>
            <small>{{ auth.role() }}</small>
          </div>
          <p-button icon="pi pi-sign-out" severity="secondary" [text]="true" [rounded]="true"
            (onClick)="logout()" ariaLabel="Salir" />
        </div>
      </aside>

      <main class="main">
        <router-outlet />
      </main>
    </div>
  `,
})
export class Shell {
  readonly auth = inject(AuthService);

  initials(): string {
    const name = this.auth.currentUser()?.fullName ?? '';
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    return (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
  }

  logout(): void {
    this.auth.logout();
    location.assign('/login');
  }
}
