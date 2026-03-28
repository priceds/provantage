import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="page-container">
      <h1 class="page-title">Dashboard</h1>
      <p class="page-subtitle">Welcome back. Here's your procurement overview.</p>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        @for (kpi of kpiCards; track kpi.label; let i = $index) {
          <div class="kpi-card glass-card hover-lift animate-fade-in-up"
               [class]="'animate-stagger-' + (i + 1)">
            <div class="kpi-header">
              <div class="kpi-icon" [style.background]="kpi.gradient">
                <mat-icon>{{ kpi.icon }}</mat-icon>
              </div>
              <div class="kpi-trend" [class.positive]="kpi.trendUp">
                <mat-icon>{{ kpi.trendUp ? 'trending_up' : 'trending_down' }}</mat-icon>
                {{ kpi.trend }}
              </div>
            </div>
            <div class="kpi-value">{{ kpi.value }}</div>
            <div class="kpi-label">{{ kpi.label }}</div>
          </div>
        }
      </div>

      <!-- Pending Actions -->
      <div class="section-row">
        <div class="section-card glass-card animate-fade-in-up animate-stagger-5">
          <h3 class="section-title">Pending Approvals</h3>
          <div class="approval-list">
            @for (item of pendingApprovals; track item.id) {
              <div class="approval-item">
                <div class="approval-info">
                  <span class="approval-number">{{ item.number }}</span>
                  <span class="approval-desc">{{ item.description }}</span>
                </div>
                <div class="approval-meta">
                  <span class="approval-amount">{{ item.amount }}</span>
                  <span class="status-badge status-pending">Pending</span>
                </div>
              </div>
            }
          </div>
        </div>

        <div class="section-card glass-card animate-fade-in-up animate-stagger-6">
          <h3 class="section-title">Recent Activity</h3>
          <div class="activity-list">
            @for (activity of recentActivity; track activity.id) {
              <div class="activity-item">
                <div class="activity-dot" [style.background]="activity.color"></div>
                <div class="activity-content">
                  <span class="activity-text">{{ activity.text }}</span>
                  <span class="activity-time">{{ activity.time }}</span>
                </div>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: $space-lg;
      margin-bottom: $space-xl;
    }

    .kpi-card {
      padding: $space-lg;
    }

    .kpi-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: $space-md;
    }

    .kpi-icon {
      width: 44px;
      height: 44px;
      border-radius: $radius-md;
      display: flex;
      align-items: center;
      justify-content: center;
      mat-icon { color: white; font-size: 22px; width: 22px; height: 22px; }
    }

    .kpi-trend {
      display: flex;
      align-items: center;
      gap: 4px;
      font-size: $text-xs;
      font-weight: 600;
      color: $color-danger;
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
      &.positive { color: $color-accent; }
    }

    .kpi-value {
      font-family: $font-heading;
      font-size: $text-3xl;
      font-weight: 700;
      color: $text-primary;
      margin-bottom: $space-xs;
    }

    .kpi-label {
      font-size: $text-sm;
      color: $text-secondary;
    }

    .section-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: $space-lg;
    }

    .section-card {
      padding: $space-lg;
    }

    .section-title {
      font-size: $text-lg;
      font-weight: 600;
      margin-bottom: $space-lg;
      color: $text-primary;
    }

    .approval-list {
      display: flex;
      flex-direction: column;
      gap: $space-md;
    }

    .approval-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: $space-md;
      border-radius: $radius-md;
      background: rgba(255, 255, 255, 0.02);
      border: 1px solid $border-subtle;
      transition: background $transition-fast;
      cursor: pointer;
      &:hover { background: rgba(255, 255, 255, 0.05); }
    }

    .approval-info {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .approval-number {
      font-weight: 600;
      font-size: $text-sm;
      color: $color-primary;
    }

    .approval-desc {
      font-size: $text-sm;
      color: $text-secondary;
    }

    .approval-meta {
      display: flex;
      align-items: center;
      gap: $space-md;
    }

    .approval-amount {
      font-weight: 700;
      font-size: $text-base;
    }

    .activity-list {
      display: flex;
      flex-direction: column;
      gap: $space-md;
    }

    .activity-item {
      display: flex;
      gap: $space-md;
      align-items: flex-start;
    }

    .activity-dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      margin-top: 6px;
      flex-shrink: 0;
    }

    .activity-content {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .activity-text {
      font-size: $text-sm;
      color: $text-primary;
    }

    .activity-time {
      font-size: $text-xs;
      color: $text-muted;
    }
  `]
})
export class DashboardComponent {
  kpiCards = [
    {
      label: 'Total Spend (MTD)',
      value: '$284,500',
      icon: 'payments',
      gradient: 'linear-gradient(135deg, hsl(250, 100%, 68%), hsl(280, 100%, 68%))',
      trend: '+12.5%',
      trendUp: true
    },
    {
      label: 'Open Purchase Orders',
      value: '23',
      icon: 'shopping_cart',
      gradient: 'linear-gradient(135deg, hsl(165, 82%, 51%), hsl(185, 82%, 51%))',
      trend: '-3',
      trendUp: false
    },
    {
      label: 'Pending Approvals',
      value: '7',
      icon: 'pending_actions',
      gradient: 'linear-gradient(135deg, hsl(38, 100%, 64%), hsl(28, 100%, 60%))',
      trend: '+2',
      trendUp: false
    },
    {
      label: 'Active Vendors',
      value: '156',
      icon: 'store',
      gradient: 'linear-gradient(135deg, hsl(210, 100%, 64%), hsl(230, 100%, 64%))',
      trend: '+8',
      trendUp: true
    }
  ];

  pendingApprovals = [
    { id: 1, number: 'PR-2026-00045', description: 'IT Equipment - Engineering Dept', amount: '$45,200' },
    { id: 2, number: 'PR-2026-00046', description: 'Office Supplies - Q2 Restock', amount: '$3,800' },
    { id: 3, number: 'PR-2026-00047', description: 'Cloud Infrastructure - AWS', amount: '$128,000' },
  ];

  recentActivity = [
    { id: 1, text: 'PO-2026-00089 sent to Acme Corp',      time: '2 min ago',  color: 'hsl(210, 100%, 64%)' },
    { id: 2, text: 'Invoice INV-4521 matched successfully',  time: '15 min ago', color: 'hsl(165, 82%, 51%)' },
    { id: 3, text: 'PR-2026-00044 approved by Jane Smith',   time: '1 hour ago', color: 'hsl(250, 100%, 68%)' },
    { id: 4, text: 'GR-2026-00032 — partial delivery received', time: '3 hours ago', color: 'hsl(38, 100%, 64%)' },
    { id: 5, text: 'Contract CON-2026-00012 expiring in 15 days', time: '5 hours ago', color: 'hsl(0, 84%, 62%)' },
  ];
}
