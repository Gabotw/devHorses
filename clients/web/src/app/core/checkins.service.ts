import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';
import { CheckIn, CheckInResult } from './models';

@Injectable({ providedIn: 'root' })
export class CheckInsService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly base = `${environment.apiBaseUrl}/checkins`;

  private connection: HubConnection | null = null;

  register(memberId: string): Observable<CheckInResult> {
    return this.http.post<CheckInResult>(this.base, { memberId, method: null });
  }

  today(): Observable<CheckIn[]> {
    return this.http.get<CheckIn[]>(`${this.base}/today`);
  }

  occupancy(): Observable<{ occupancy: number }> {
    return this.http.get<{ occupancy: number }>(`${this.base}/occupancy`);
  }

  /** Se conecta al hub de aforo y llama `onChange` con cada nuevo conteo. */
  async connectOccupancy(onChange: (occupancy: number) => void): Promise<void> {
    if (this.connection) return;

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.hubBaseUrl}/occupancy`, {
        accessTokenFactory: () => this.auth.token ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('occupancyChanged', (occupancy: number) => onChange(occupancy));
    await this.connection.start();
  }

  async disconnectOccupancy(): Promise<void> {
    if (this.connection && this.connection.state !== HubConnectionState.Disconnected) {
      await this.connection.stop();
    }
    this.connection = null;
  }
}
