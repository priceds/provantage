import {
  AfterViewInit, Component, ElementRef, OnDestroy, OnInit,
  ViewChild, effect, inject, signal
} from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import {
  Chart, CategoryScale, LinearScale, BarElement, LineElement,
  PointElement, Tooltip, Legend, Filler, BarController, LineController
} from 'chart.js';
import { DashboardKpis, DashboardService } from '../../core/services/dashboard.service';
import { SignalRService } from '../../core/services/signalr.service';

// Register only the Chart.js components we need
Chart.register(
  CategoryScale, LinearScale, BarElement, LineElement,
  PointElement, Tooltip, Legend, Filler, BarController, LineController
);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, MatIconModule, RouterModule, CurrencyPipe],
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Dashboard</h1>
          <p class="page-subtitle">Procurement overview — real-time data</p>
        </div>
        @if (loading()) {
          <div class="loading-badge">
            <mat-icon class="spin">sync</mat-icon> Refreshing…
          </div>
        }
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card glass-card hover-lift animate-fade-in-up animate-stagger-1">
          <div class="kpi-header">
            <div class="kpi-icon" style="background: linear-gradient(135deg, hsl(250,100%,68%), hsl(280,100%,68%))">
              <mat-icon>payments</mat-icon>
            </div>
            <div class="kpi-trend" [class.positive]="(kpis()?.spendChangePct ?? 0) >= 0">
              <mat-icon>{{ (kpis()?.spendChangePct ?? 0) >= 0 ? 'trending_up' : 'trending_down' }}</mat-icon>
              {{ kpis()?.spendChangePct | number:'1.1-1' }}%
            </div>
          </div>
          <div class="kpi-value">{{ kpis()?.totalSpendMtd | currency:(kpis()?.currency ?? 'USD'):'symbol':'1.0-0' }}</div>
          <div class="kpi-label">Total Spend (MTD)</div>
        </div>

        <div class="kpi-card glass-card hover-lift animate-fade-in-up animate-stagger-2">
          <div class="kpi-header">
            <div class="kpi-icon" style="background: linear-gradient(135deg, hsl(165,82%,51%), hsl(185,82%,51%))">
              <mat-icon>shopping_cart</mat-icon>
            </div>
          </div>
          <div class="kpi-value">{{ kpis()?.openPurchaseOrders ?? '—' }}</div>
          <div class="kpi-label">Open Purchase Orders</div>
        </div>

        <div class="kpi-card glass-card hover-lift animate-fade-in-up animate-stagger-3">
          <div class="kpi-header">
            <div class="kpi-icon" style="background: linear-gradient(135deg, hsl(38,100%,64%), hsl(28,100%,60%))">
              <mat-icon>pending_actions</mat-icon>
            </div>
          </div>
          <div class="kpi-value">{{ kpis()?.pendingApprovals ?? '—' }}</div>
          <div class="kpi-label">Pending Approvals</div>
        </div>

        <div class="kpi-card glass-card hover-lift animate-fade-in-up animate-stagger-4">
          <div class="kpi-header">
            <div class="kpi-icon" style="background: linear-gradient(135deg, hsl(210,100%,64%), hsl(230,100%,64%))">
              <mat-icon>store</mat-icon>
            </div>
          </div>
          <div class="kpi-value">{{ kpis()?.activeVendors ?? '—' }}</div>
          <div class="kpi-label">Active Vendors</div>
        </div>
      </div>

      <!-- Charts Row -->
      <div class="charts-row animate-fade-in-up animate-stagger-5">
        <!-- Spend Trend Chart -->
        <div class="chart-card glass-card">
          <h3 class="section-title">6-Month Spend Trend</h3>
          <div class="chart-wrapper">
            <canvas #spendTrendChart></canvas>
          </div>
        </div>

        <!-- Budget Utilization -->
        <div class="chart-card glass-card">
          <h3 class="section-title">Budget Utilization</h3>
          <div class="budget-util-display">
            <div class="util-ring-wrap">
              <svg viewBox="0 0 120 120" class="util-ring">
                <circle cx="60" cy="60" r="52" class="ring-bg"/>
                <circle cx="60" cy="60" r="52" class="ring-fill"
                  [attr.stroke-dasharray]="ringDash()"
                  [attr.stroke]="ringColor()" />
              </svg>
              <div class="util-label">
                <span class="util-pct">{{ kpis()?.budgetUtilizationAvg | number:'1.0-1' }}%</span>
                <span class="util-sub">avg used</span>
              </div>
            </div>
            <div class="util-legend">
              <div class="util-legend-item">
                <span class="dot" style="background:hsl(250,100%,68%)"></span> Allocated
              </div>
              <div class="util-legend-item">
                <span class="dot" style="background:hsl(38,100%,64%)"></span> Committed
              </div>
              <div class="util-legend-item">
                <span class="dot" style="background:hsl(165,82%,51%)"></span> Spent
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Lower Row -->
      <div class="section-row animate-fade-in-up animate-stagger-6">
        <!-- Pending Approvals -->
        <div class="section-card glass-card">
          <h3 class="section-title">Pending Approvals</h3>
          @if (kpis()?.pendingApprovalsList?.length === 0) {
            <p class="empty-msg">No pending approvals.</p>
          }
          <div class="approval-list">
            @for (item of kpis()?.pendingApprovalsList ?? []; track item.id) {
              <div class="approval-item">
                <div class="approval-info">
                  <span class="approval-number">{{ item.number }}</span>
                  <span class="approval-desc">{{ item.title }} — {{ item.department }}</span>
                </div>
                <div class="approval-meta">
                  <span class="approval-amount">
                    {{ item.amount | currency:(item.currency ?? 'USD'):'symbol':'1.0-0' }}
                  </span>
                  <span class="status-badge status-pending">Pending</span>
                </div>
              </div>
            }
          </div>
        </div>

        <!-- Recent Activity -->
        <div class="section-card glass-card">
          <h3 class="section-title">Recent Activity</h3>
          <div class="activity-list">
            @for (item of kpis()?.recentActivity ?? []; track item.id) {
              <div class="activity-item">
                <div class="activity-dot" [style.background]="item.color"></div>
                <div class="activity-content">
                  <span class="activity-text">{{ item.text }}</span>
                  <span class="activity-time">{{ item.timeAgo }}</span>
                </div>
              </div>
            }
            @if ((kpis()?.recentActivity?.length ?? 0) === 0) {
              <p class="empty-msg">No recent activity.</p>
            }
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: $space-xl;
    }

    .loading-badge {
      display: flex;
      align-items: center;
      gap: $space-xs;
      font-size: $text-sm;
      color: $text-secondary;
      .spin { animation: spin 1s linear infinite; font-size: 16px; width: 16px; height: 16px; }
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: $space-lg;
      margin-bottom: $space-xl;
    }

    .kpi-card { padding: $space-lg; }

    .kpi-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: $space-md;
    }

    .kpi-icon {
      width: 44px; height: 44px;
      border-radius: $radius-md;
      display: flex; align-items: center; justify-content: center;
      mat-icon { color: white; font-size: 22px; width: 22px; height: 22px; }
    }

    .kpi-trend {
      display: flex; align-items: center; gap: 4px;
      font-size: $text-xs; font-weight: 600; color: $color-danger;
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
      &.positive { color: $color-accent; }
    }

    .kpi-value {
      font-family: $font-heading; font-size: $text-3xl;
      font-weight: 700; color: $text-primary; margin-bottom: $space-xs;
    }

    .kpi-label { font-size: $text-sm; color: $text-secondary; }

    /* Charts */
    .charts-row {
      display: grid;
      grid-template-columns: 2fr 1fr;
      gap: $space-lg;
      margin-bottom: $space-xl;
    }

    .chart-card { padding: $space-lg; }

    .chart-wrapper { height: 220px; position: relative; }

    /* Budget ring */
    .budget-util-display {
      display: flex;
      align-items: center;
      gap: $space-xl;
      padding: $space-md 0;
    }

    .util-ring-wrap {
      position: relative;
      width: 140px; height: 140px; flex-shrink: 0;
    }

    .util-ring {
      width: 100%; height: 100%;
      transform: rotate(-90deg);
    }

    .ring-bg {
      fill: none;
      stroke: rgba(255,255,255,0.06);
      stroke-width: 12;
    }

    .ring-fill {
      fill: none;
      stroke-width: 12;
      stroke-linecap: round;
      transition: stroke-dasharray 0.6s ease;
    }

    .util-label {
      position: absolute;
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
      text-align: center;
    }

    .util-pct {
      display: block;
      font-family: $font-heading;
      font-size: $text-2xl;
      font-weight: 700;
      color: $text-primary;
    }

    .util-sub { font-size: $text-xs; color: $text-muted; }

    .util-legend { display: flex; flex-direction: column; gap: $space-sm; }
    .util-legend-item {
      display: flex; align-items: center; gap: $space-sm;
      font-size: $text-sm; color: $text-secondary;
    }
    .dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }

    /* Lower row */
    .section-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: $space-lg;
    }

    .section-card { padding: $space-lg; }
    .section-title {
      font-size: $text-lg; font-weight: 600;
      margin-bottom: $space-lg; color: $text-primary;
    }

    .approval-list { display: flex; flex-direction: column; gap: $space-md; }

    .approval-item {
      display: flex; justify-content: space-between; align-items: center;
      padding: $space-md; border-radius: $radius-md;
      background: rgba(255,255,255,0.02); border: 1px solid $border-subtle;
      transition: background $transition-fast; cursor: pointer;
      &:hover { background: rgba(255,255,255,0.05); }
    }

    .approval-info { display: flex; flex-direction: column; gap: 4px; }
    .approval-number { font-weight: 600; font-size: $text-sm; color: $color-primary; }
    .approval-desc { font-size: $text-sm; color: $text-secondary; }
    .approval-meta { display: flex; align-items: center; gap: $space-md; }
    .approval-amount { font-weight: 700; font-size: $text-base; }

    .activity-list { display: flex; flex-direction: column; gap: $space-md; }

    .activity-item { display: flex; gap: $space-md; align-items: flex-start; }
    .activity-dot {
      width: 8px; height: 8px; border-radius: 50%;
      margin-top: 6px; flex-shrink: 0;
    }
    .activity-content { display: flex; flex-direction: column; gap: 2px; }
    .activity-text { font-size: $text-sm; color: $text-primary; }
    .activity-time { font-size: $text-xs; color: $text-muted; }

    .empty-msg { font-size: $text-sm; color: $text-muted; padding: $space-md 0; }
  `]
})
export class DashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('spendTrendChart') spendChartRef!: ElementRef<HTMLCanvasElement>;

  private dashboardService = inject(DashboardService);
  private signalR = inject(SignalRService);

  kpis = signal<DashboardKpis | null>(null);
  loading = signal(false);

  private spendChart: Chart | null = null;

  constructor() {
    // Re-fetch when backend signals a dashboard refresh
    effect(() => {
      const tick = this.signalR.dashboardRefreshTick();
      if (tick > 0) this.loadKpis();
    });
  }

  ngOnInit(): void {
    this.loadKpis();
  }

  ngAfterViewInit(): void {
    this.buildSpendChart([]);
  }

  ngOnDestroy(): void {
    this.spendChart?.destroy();
  }

  ringDash(): string {
    const pct = this.kpis()?.budgetUtilizationAvg ?? 0;
    const circumference = 2 * Math.PI * 52; // r=52
    const filled = (Math.min(pct, 100) / 100) * circumference;
    return `${filled} ${circumference}`;
  }

  ringColor(): string {
    const pct = this.kpis()?.budgetUtilizationAvg ?? 0;
    if (pct >= 90) return 'hsl(0, 84%, 62%)';
    if (pct >= 75) return 'hsl(38, 100%, 64%)';
    return 'hsl(250, 100%, 68%)';
  }

  private loadKpis(): void {
    this.loading.set(true);
    this.dashboardService.getKpis().subscribe({
      next: data => {
        this.kpis.set(data);
        this.loading.set(false);
        this.updateSpendChart(data);
      },
      error: () => this.loading.set(false)
    });
  }

  private buildSpendChart(points: { month: string; amount: number }[]): void {
    if (!this.spendChartRef) return;
    const ctx = this.spendChartRef.nativeElement.getContext('2d')!;

    this.spendChart?.destroy();

    const gradient = ctx.createLinearGradient(0, 0, 0, 200);
    gradient.addColorStop(0, 'rgba(124,58,237,0.35)');
    gradient.addColorStop(1, 'rgba(124,58,237,0)');

    this.spendChart = new Chart(ctx, {
      type: 'line',
      data: {
        labels: points.map(p => p.month),
        datasets: [{
          label: 'Spend',
          data: points.map(p => p.amount),
          fill: true,
          backgroundColor: gradient,
          borderColor: 'hsl(250, 100%, 68%)',
          borderWidth: 2,
          pointRadius: 4,
          pointBackgroundColor: 'hsl(250, 100%, 68%)',
          tension: 0.4
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(15,12,31,0.95)',
            borderColor: 'rgba(255,255,255,0.1)',
            borderWidth: 1,
            callbacks: {
              label: (ctx: any) => ` $${Number(ctx.parsed?.y ?? 0).toLocaleString()}`
            }
          }
        },
        scales: {
          x: {
            grid: { color: 'rgba(255,255,255,0.04)' },
            ticks: { color: 'rgba(255,255,255,0.5)', font: { size: 12 } }
          },
          y: {
            grid: { color: 'rgba(255,255,255,0.04)' },
            ticks: {
              color: 'rgba(255,255,255,0.5)', font: { size: 12 },
              callback: (value: string | number) => `$${Number(value).toLocaleString()}`
            }
          }
        }
      }
    });
  }

  private updateSpendChart(data: DashboardKpis): void {
    const points = data.spendTrend ?? [];
    if (this.spendChart) {
      this.spendChart.data.labels = points.map(p => p.month);
      this.spendChart.data.datasets[0].data = points.map(p => p.amount);
      this.spendChart.update('active');
    } else {
      this.buildSpendChart(points);
    }
  }
}
