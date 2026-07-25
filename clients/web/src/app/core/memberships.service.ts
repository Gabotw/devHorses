import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Membership } from './models';

@Injectable({ providedIn: 'root' })
export class MembershipsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/memberships`;

  create(memberId: string, planId: string, startDate?: string): Observable<Membership> {
    return this.http.post<Membership>(this.base, { memberId, planId, startDate: startDate ?? null });
  }

  freeze(id: string, from: string, until: string): Observable<Membership> {
    return this.http.post<Membership>(`${this.base}/${id}/freeze`, { from, until });
  }

  unfreeze(id: string, resumeDate: string): Observable<Membership> {
    return this.http.post<Membership>(`${this.base}/${id}/unfreeze`, { resumeDate });
  }
}
