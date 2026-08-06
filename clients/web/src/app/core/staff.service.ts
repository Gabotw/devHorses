import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { StaffRole, StaffUser } from './models';

export interface CreateStaffPayload {
  fullName: string;
  email: string;
  password: string;
  role: StaffRole;
}

export interface UpdateStaffPayload {
  fullName: string;
  role: StaffRole;
}

@Injectable({ providedIn: 'root' })
export class StaffService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/staff`;

  list(): Observable<StaffUser[]> {
    return this.http.get<StaffUser[]>(this.base);
  }

  create(payload: CreateStaffPayload): Observable<StaffUser> {
    return this.http.post<StaffUser>(this.base, payload);
  }

  update(id: string, payload: UpdateStaffPayload): Observable<StaffUser> {
    return this.http.put<StaffUser>(`${this.base}/${id}`, payload);
  }

  resetPassword(id: string, password: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reset-password`, { password });
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, {});
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, {});
  }
}
