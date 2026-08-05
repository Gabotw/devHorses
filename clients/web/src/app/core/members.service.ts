import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Member, Membership, PagedResult } from './models';

export interface MemberPayload {
  fullName: string;
  documentId: string;
  phone?: string | null;
  email?: string | null;
  photoUrl?: string | null;
}

@Injectable({ providedIn: 'root' })
export class MembersService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/members`;

  list(search: string, page: number, pageSize: number): Observable<PagedResult<Member>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Member>>(this.base, { params });
  }

  get(id: string): Observable<Member> {
    return this.http.get<Member>(`${this.base}/${id}`);
  }

  create(payload: MemberPayload): Observable<Member> {
    return this.http.post<Member>(this.base, payload);
  }

  update(id: string, payload: MemberPayload): Observable<Member> {
    return this.http.put<Member>(`${this.base}/${id}`, payload);
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/deactivate`, {});
  }

  activate(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/activate`, {});
  }

  regenerateCode(id: string): Observable<Member> {
    return this.http.post<Member>(`${this.base}/${id}/regenerate-code`, {});
  }

  memberships(id: string): Observable<Membership[]> {
    return this.http.get<Membership[]>(`${this.base}/${id}/memberships`);
  }
}
