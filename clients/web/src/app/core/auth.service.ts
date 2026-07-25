import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResult } from './models';

const TOKEN_KEY = 'gymflow.token';
const USER_KEY = 'gymflow.user';

interface StoredSession {
  token: string;
  fullName: string;
  role: string;
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly session = signal<StoredSession | null>(this.restore());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly currentUser = computed(() => this.session());
  readonly role = computed(() => this.session()?.role ?? null);

  /** ¿El usuario puede gestionar (Owner/Admin)? Controla acciones sensibles en la UI. */
  readonly isManager = computed(() => {
    const r = this.session()?.role;
    return r === 'Owner' || r === 'Admin';
  });

  get token(): string | null {
    return this.session()?.token ?? null;
  }

  login(subdomain: string, email: string, password: string): Observable<LoginResult> {
    const headers = new HttpHeaders({ 'X-Tenant-Subdomain': subdomain.trim().toLowerCase() });
    return this.http
      .post<LoginResult>(`${environment.apiBaseUrl}/auth/login`, { email, password }, { headers })
      .pipe(
        tap((res) => {
          const s: StoredSession = {
            token: res.accessToken,
            fullName: res.fullName,
            role: res.role,
            userId: res.userId,
          };
          this.persist(s);
          this.session.set(s);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.session.set(null);
  }

  private persist(s: StoredSession): void {
    localStorage.setItem(TOKEN_KEY, s.token);
    localStorage.setItem(USER_KEY, JSON.stringify({ fullName: s.fullName, role: s.role, userId: s.userId }));
  }

  private restore(): StoredSession | null {
    const token = localStorage.getItem(TOKEN_KEY);
    const rawUser = localStorage.getItem(USER_KEY);
    if (!token || !rawUser) return null;
    try {
      const u = JSON.parse(rawUser);
      return { token, fullName: u.fullName, role: u.role, userId: u.userId };
    } catch {
      return null;
    }
  }
}
