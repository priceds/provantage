import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface SignalRNotification {
  id?: string;
  title: string;
  message: string;
  type: string;
  actionUrl?: string;
  createdAt?: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private readonly hubBaseUrl = '/hubs';
  private notificationConn: signalR.HubConnection | null = null;
  private dashboardConn: signalR.HubConnection | null = null;

  /** Emits each incoming notification payload. */
  readonly latestNotification = signal<SignalRNotification | null>(null);

  /** Increments every time the backend requests a dashboard refresh. */
  readonly dashboardRefreshTick = signal(0);

  /** True while both connections are active. */
  readonly connected = signal(false);

  async connect(token: string): Promise<void> {
    if (this.connected()) return;

    const tokenFactory = () => token;

    this.notificationConn = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubBaseUrl}/notifications`, { accessTokenFactory: tokenFactory })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.dashboardConn = new signalR.HubConnectionBuilder()
      .withUrl(`${this.hubBaseUrl}/dashboard`, { accessTokenFactory: tokenFactory })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.notificationConn.on('ReceiveNotification', (payload: SignalRNotification) => {
      this.latestNotification.set(payload);
    });

    this.dashboardConn.on('DashboardRefresh', () => {
      this.dashboardRefreshTick.update(v => v + 1);
    });

    await Promise.all([
      this.notificationConn.start(),
      this.dashboardConn.start()
    ]);

    this.connected.set(true);
  }

  async disconnect(): Promise<void> {
    await Promise.all([
      this.notificationConn?.stop(),
      this.dashboardConn?.stop()
    ]);
    this.connected.set(false);
  }
}
