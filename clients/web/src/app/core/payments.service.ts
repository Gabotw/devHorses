import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Payment } from './models';

export interface CashPaymentPayload {
  memberId: string;
  membershipId?: string | null;
  amount: number;
  notes?: string | null;
}

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/payments`;

  listByMember(memberId: string): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.base}/by-member/${memberId}`);
  }

  registerCash(payload: CashPaymentPayload): Observable<Payment> {
    return this.http.post<Payment>(`${this.base}/cash`, payload);
  }
}
