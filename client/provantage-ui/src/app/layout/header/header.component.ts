import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/auth/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatBadgeModule, MatMenuModule, MatButtonModule, MatDividerModule],
  template: `
    <header class="header">
      <!-- Search Bar -->
      <div class="search-wrapper">
        <mat-icon class="search-icon">search</mat-icon>
        <input
          type="text"
          class="search-input"
          placeholder="Search vendors, orders, invoices..."
          id="global-search" />
      </div>

      <div class="header-actions">
        <!-- Notifications -->
        <button
          mat-icon-button
          class="header-btn"
          [matMenuTriggerFor]="notifMenu"
          [matBadge]="notificationService.unreadCount()"
          [matBadgeHidden]="notificationService.unreadCount() === 0"
          matBadgeColor="warn"
          matBadgeSize="small"
          id="notification-bell"
          (click)="onNotifOpen()">
          <mat-icon>notifications_none</mat-icon>
        </button>

        <!-- Notification Panel -->
        <mat-menu #notifMenu="matMenu" class="notif-menu" xPosition="before">
          <div class="notif-header" (click)="$event.stopPropagation()">
            <span class="notif-title">Notifications</span>
            @if (notificationService.unreadCount() > 0) {
              <button class="mark-all-btn" (click)="markAllRead()">Mark all read</button>
            }
          </div>
          <mat-divider></mat-divider>

          <div class="notif-list" (click)="$event.stopPropagation()">
            @for (n of notificationService.notifications(); track n.id) {
              <div class="notif-item" [class.unread]="!n.isRead" (click)="markRead(n.id)">
                <div class="notif-dot" [class]="'dot-' + n.type.toLowerCase()"></div>
                <div class="notif-body">
                  <div class="notif-item-title">{{ n.title }}</div>
                  <div class="notif-item-msg">{{ n.message }}</div>
                  <div class="notif-time">{{ n.timeAgo }}</div>
                </div>
              </div>
            }
            @if (notificationService.notifications().length === 0) {
              <div class="notif-empty">No notifications</div>
            }
          </div>
        </mat-menu>

        <!-- User Menu -->
        <button
          mat-button
          [matMenuTriggerFor]="userMenu"
          class="user-menu-trigger"
          id="user-menu-btn">
          <div class="user-avatar">{{ userInitials() }}</div>
          <div class="user-info">
            <span class="user-name">{{ userName() }}</span>
            <span class="user-role">{{ userRole() }}</span>
          </div>
          <mat-icon>expand_more</mat-icon>
        </button>

        <mat-menu #userMenu="matMenu" xPosition="before">
          <button mat-menu-item>
            <mat-icon>person</mat-icon> Profile
          </button>
          <button mat-menu-item>
            <mat-icon>settings</mat-icon> Settings
          </button>
          <mat-divider></mat-divider>
          <button mat-menu-item (click)="signOut()">
            <mat-icon>logout</mat-icon> Sign Out
          </button>
        </mat-menu>
      </div>
    </header>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .header {
      height: $header-height;
      background: $bg-secondary;
      border-bottom: 1px solid $border-subtle;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 $space-xl;
    }

    .search-wrapper {
      display: flex;
      align-items: center;
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      padding: $space-sm $space-md;
      width: 380px;
      transition: all $transition-fast;

      &:focus-within {
        border-color: $color-primary;
        background: rgba(255, 255, 255, 0.06);
        box-shadow: 0 0 0 3px rgba($color-primary, 0.1);
      }
    }

    .search-icon { color: $text-muted; font-size: 20px; margin-right: $space-sm; }

    .search-input {
      background: none; border: none; outline: none;
      color: $text-primary; font-size: $text-sm; width: 100%;
      &::placeholder { color: $text-muted; }
    }

    .header-actions { display: flex; align-items: center; gap: $space-sm; }

    .header-btn { color: $text-secondary; &:hover { color: $text-primary; } }

    /* Notification panel */
    .notif-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: $space-md $space-lg;
    }

    .notif-title { font-weight: 600; font-size: $text-base; color: $text-primary; }

    .mark-all-btn {
      background: none; border: none; cursor: pointer;
      font-size: $text-xs; color: $color-primary;
      &:hover { text-decoration: underline; }
    }

    .notif-list {
      max-height: 340px;
      overflow-y: auto;
      min-width: 320px;
    }

    .notif-item {
      display: flex;
      gap: $space-sm;
      padding: $space-md $space-lg;
      cursor: pointer;
      transition: background $transition-fast;
      border-bottom: 1px solid $border-subtle;

      &:hover { background: rgba(255,255,255,0.04); }
      &.unread { background: rgba(124, 58, 237, 0.06); }
      &:last-child { border-bottom: none; }
    }

    .notif-dot {
      width: 8px; height: 8px; border-radius: 50%;
      margin-top: 6px; flex-shrink: 0;
      background: $text-muted;

      &.dot-warning { background: hsl(38,100%,64%); }
      &.dot-success { background: hsl(165,82%,51%); }
      &.dot-error { background: hsl(0,84%,62%); }
      &.dot-approvalrequired { background: hsl(250,100%,68%); }
      &.dot-info { background: hsl(210,100%,64%); }
    }

    .notif-body { display: flex; flex-direction: column; gap: 2px; flex: 1; }

    .notif-item-title { font-size: $text-sm; font-weight: 600; color: $text-primary; }
    .notif-item-msg { font-size: $text-xs; color: $text-secondary; line-height: 1.4; }
    .notif-time { font-size: 11px; color: $text-muted; margin-top: 2px; }

    .notif-empty {
      padding: $space-xl;
      text-align: center;
      font-size: $text-sm;
      color: $text-muted;
    }

    .user-menu-trigger {
      display: flex; align-items: center; gap: $space-sm;
      padding: $space-xs $space-sm; border-radius: $radius-md;
      color: $text-primary;
    }

    .user-avatar {
      width: 32px; height: 32px; border-radius: 50%;
      background: linear-gradient(135deg, $color-primary, $color-accent);
      display: flex; align-items: center; justify-content: center;
      font-weight: 700; font-size: $text-xs; color: white;
    }

    .user-info { display: flex; flex-direction: column; text-align: left; line-height: 1.2; }
    .user-name { font-size: $text-sm; font-weight: 600; }
    .user-role { font-size: 11px; color: $text-secondary; }
  `]
})
export class HeaderComponent implements OnInit {
  notificationService = inject(NotificationService);
  private authService = inject(AuthService);
  private router = inject(Router);

  userName = signal('');
  userRole = signal('');
  userInitials = signal('');

  ngOnInit(): void {
    const user = this.authService.currentUser();
    if (user) {
      const name = `${user.firstName} ${user.lastName}`;
      this.userName.set(name);
      this.userRole.set(user.role);
      this.userInitials.set(`${user.firstName[0]}${user.lastName[0]}`.toUpperCase());
    }
    this.notificationService.loadNotifications();
  }

  onNotifOpen(): void {
    this.notificationService.loadNotifications();
  }

  markRead(id: string): void {
    this.notificationService.markRead(id);
  }

  markAllRead(): void {
    this.notificationService.markAllRead();
  }

  signOut(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
