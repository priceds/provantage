import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatBadgeModule } from '@angular/material/badge';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';

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
          [matBadge]="notificationCount()"
          [matBadgeHidden]="notificationCount() === 0"
          matBadgeColor="warn"
          matBadgeSize="small"
          id="notification-bell">
          <mat-icon>notifications_none</mat-icon>
        </button>

        <!-- User Menu -->
        <button
          mat-button
          [matMenuTriggerFor]="userMenu"
          class="user-menu-trigger"
          id="user-menu-btn">
          <div class="user-avatar">SA</div>
          <div class="user-info">
            <span class="user-name">Sarvesh Admin</span>
            <span class="user-role">Administrator</span>
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
          <button mat-menu-item>
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

    .search-icon {
      color: $text-muted;
      font-size: 20px;
      margin-right: $space-sm;
    }

    .search-input {
      background: none;
      border: none;
      outline: none;
      color: $text-primary;
      font-size: $text-sm;
      width: 100%;

      &::placeholder { color: $text-muted; }
    }

    .header-actions {
      display: flex;
      align-items: center;
      gap: $space-sm;
    }

    .header-btn {
      color: $text-secondary;
      &:hover { color: $text-primary; }
    }

    .user-menu-trigger {
      display: flex;
      align-items: center;
      gap: $space-sm;
      padding: $space-xs $space-sm;
      border-radius: $radius-md;
      color: $text-primary;
    }

    .user-avatar {
      width: 32px;
      height: 32px;
      border-radius: 50%;
      background: linear-gradient(135deg, $color-primary, $color-accent);
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: $text-xs;
      color: white;
    }

    .user-info {
      display: flex;
      flex-direction: column;
      text-align: left;
      line-height: 1.2;
    }

    .user-name {
      font-size: $text-sm;
      font-weight: 600;
    }

    .user-role {
      font-size: 11px;
      color: $text-secondary;
    }
  `]
})
export class HeaderComponent {
  notificationCount = signal(5);
}
