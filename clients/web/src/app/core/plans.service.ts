import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Plan } from './models';

export interface PlanPayload {
  name: string;
  price: number;
  durationDays: number;
  monthlyAccesses?: number | null;
}

@Injectable({ providedIn: 'root' })
export class PlansService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/plans`;

  list(includeInactive = false): Observable<Plan[]> {
    const params = new HttpParams().set('includeInactive', includeInactive);
    return this.http.get<Plan[]>(this.base, { params });
  }

  create(payload: PlanPayload): Observable<Plan> {
    return this.http.post<Plan>(this.base, payload);
  }

  update(id: string, payload: PlanPayload): Observable<Plan> {
    return this.http.put<Plan>(`${this.base}/${id}`, payload);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, {});
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, {});
  }
}
