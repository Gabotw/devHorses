import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Dashboard } from './models';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/reports`;

  /** Dashboard del tenant. `from`/`to` son fechas locales (yyyy-MM-dd); si se omiten, últimos 30 días. */
  dashboard(from?: string, to?: string): Observable<Dashboard> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<Dashboard>(`${this.base}/dashboard`, { params });
  }
}
