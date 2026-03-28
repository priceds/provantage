import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  badge?: number;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatTooltipModule],
  template: `
    <aside class="sidebar" [class.collapsed]="isCollapsed()">
      <!-- Logo -->
      <div class="sidebar-logo">
        <div class="logo-icon">P</div>
        <span class="logo-text" *ngIf="!isCollapsed()">ProVantage</span>
      </div>

      <!-- Navigation -->
      <nav class="sidebar-nav">
        @for (item of navItems; track item.route) {
          <a
            [routerLink]="item.route"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: item.route === '/dashboard' }"
            class="nav-item"
            [matTooltip]="isCollapsed() ? item.label : ''"
            matTooltipPosition="right">
            <mat-icon class="nav-icon">{{ item.icon }}</mat-icon>
            <span class="nav-label" *ngIf="!isCollapsed()">{{ item.label }}</span>
            @if (item.badge && item.badge > 0) {
              <span class="nav-badge">{{ item.badge }}</span>
            }
          </a>
        }
      </nav>

      <!-- Collapse Toggle -->
      <button class="collapse-toggle" (click)="toggleCollapse()">
        <mat-icon>{{ isCollapsed() ? 'chevron_right' : 'chevron_left' }}</mat-icon>
      </button>
    </aside>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .sidebar {
      width: $sidebar-width;
      height: 100vh;
      background: $bg-secondary;
      border-right: 1px solid $border-subtle;
      display: flex;
      flex-direction: column;
      transition: width $transition-smooth;
      position: fixed;
      left: 0;
      top: 0;
      z-index: 100;
      overflow: hidden;

      &.collapsed { width: $sidebar-collapsed; }
    }

    .sidebar-logo {
      display: flex;
      align-items: center;
      gap: $space-md;
      padding: $space-lg;
      border-bottom: 1px solid $border-subtle;
      height: $header-height;
    }

    .logo-icon {
      width: 36px;
      height: 36px;
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 15%));
      border-radius: $radius-md;
      display: flex;
      align-items: center;
      justify-content: center;
      font-family: $font-heading;
      font-weight: 800;
      font-size: $text-lg;
      color: white;
      flex-shrink: 0;
    }

    .logo-text {
      font-family: $font-heading;
      font-weight: 700;
      font-size: $text-xl;
      color: $text-primary;
      white-space: nowrap;
    }

    .sidebar-nav {
      flex: 1;
      padding: $space-md $space-sm;
      display: flex;
      flex-direction: column;
      gap: 2px;
      overflow-y: auto;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: $space-md;
      padding: $space-sm $space-md;
      border-radius: $radius-md;
      color: $text-secondary;
      text-decoration: none;
      transition: all $transition-fast;
      position: relative;
      white-space: nowrap;

      &:hover {
        background: rgba(255, 255, 255, 0.05);
        color: $text-primary;
      }

      &.active {
        background: rgba($color-primary, 0.12);
        color: $color-primary;
        .nav-icon { color: $color-primary; }
      }
    }

    .nav-icon {
      font-size: 22px;
      width: 22px;
      height: 22px;
      flex-shrink: 0;
    }

    .nav-label {
      font-size: $text-sm;
      font-weight: 500;
    }

    .nav-badge {
      margin-left: auto;
      background: $color-danger;
      color: white;
      font-size: 11px;
      font-weight: 700;
      padding: 2px 7px;
      border-radius: $radius-pill;
      line-height: 1;
    }

    .collapse-toggle {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: $space-md;
      border: none;
      background: none;
      color: $text-muted;
      cursor: pointer;
      border-top: 1px solid $border-subtle;
      transition: color $transition-fast;
      &:hover { color: $text-primary; }
    }
  `]
})
export class SidebarComponent {
  isCollapsed = signal(false);

  navItems: NavItem[] = [
    { label: 'Dashboard',     icon: 'dashboard',          route: '/dashboard' },
    { label: 'Vendors',       icon: 'store',              route: '/vendors' },
    { label: 'Requisitions',  icon: 'assignment',         route: '/requisitions', badge: 3 },
    { label: 'Purchase Orders', icon: 'shopping_cart',    route: '/purchase-orders' },
    { label: 'Invoices',      icon: 'receipt_long',       route: '/invoices' },
    { label: 'Goods Receipts', icon: 'inventory_2',       route: '/goods-receipts' },
    { label: 'Contracts',     icon: 'description',        route: '/contracts' },
    { label: 'Budgets',       icon: 'account_balance',    route: '/budgets' },
    { label: 'Analytics',     icon: 'analytics',          route: '/analytics' },
    { label: 'Audit Logs',    icon: 'history',            route: '/audit-logs' },
    { label: 'Settings',      icon: 'settings',           route: '/settings' },
  ];

  toggleCollapse() {
    this.isCollapsed.update(v => !v);
  }
}
