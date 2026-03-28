import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SignalRService } from './signalr.service';

export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  actionUrl: string | null;
  entityType: string | null;
  entityId: string | null;
  createdAt: string;
  timeAgo: string;
}

export interface PaginatedNotifications {
  items: NotificationDto[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private signalR = inject(SignalRService);

  readonly notifications = signal<NotificationDto[]>([]);
  readonly unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  constructor() {
    // When a real-time notification arrives, prepend it and bump the count
    effect(() => {
      const incoming = this.signalR.latestNotification();
      if (!incoming) return;
      const dto: NotificationDto = {
        id: incoming.id ?? crypto.randomUUID(),
        title: incoming.title,
        message: incoming.message,
        type: incoming.type,
        isRead: false,
        actionUrl: incoming.actionUrl ?? null,
        entityType: null,
        entityId: null,
        createdAt: incoming.createdAt ?? new Date().toISOString(),
        timeAgo: 'just now'
      };
      this.notifications.update(list => [dto, ...list]);
    });
  }

  loadNotifications(unreadOnly = false): void {
    this.http
      .get<PaginatedNotifications>(`/api/notifications?pageSize=20&unreadOnly=${unreadOnly}`)
      .subscribe(data => this.notifications.set(data.items));
  }

  markRead(id: string): void {
    this.http.post(`/api/notifications/${id}/read`, {}).subscribe(() => {
      this.notifications.update(list =>
        list.map(n => n.id === id ? { ...n, isRead: true } : n)
      );
    });
  }

  markAllRead(): void {
    this.http.post('/api/notifications/read-all', {}).subscribe(() => {
      this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
    });
  }
}
