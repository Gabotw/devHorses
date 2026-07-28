import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ClassReservation, ClassSession, CreateClassSessionPayload } from './models';

@Injectable({ providedIn: 'root' })
export class ClassesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/classes`;

  /** Agenda de clases; `from`/`to` son fechas locales (yyyy-MM-dd). */
  list(from?: string, to?: string): Observable<ClassSession[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<ClassSession[]>(this.base, { params });
  }

  create(payload: CreateClassSessionPayload): Observable<ClassSession> {
    return this.http.post<ClassSession>(this.base, payload);
  }

  cancel(sessionId: string): Observable<ClassSession> {
    return this.http.post<ClassSession>(`${this.base}/${sessionId}/cancel`, {});
  }

  roster(sessionId: string): Observable<ClassReservation[]> {
    return this.http.get<ClassReservation[]>(`${this.base}/${sessionId}/roster`);
  }

  markAttendance(sessionId: string, memberId: string): Observable<ClassReservation> {
    return this.http.post<ClassReservation>(`${this.base}/${sessionId}/attendance/${memberId}`, {});
  }
}
