import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import {
  ArcElement,
  Chart,
  DoughnutController,
  Legend,
  Tooltip
} from 'chart.js';
import {
  AnalyticsService,
  SpendByCategoryDto,
  VendorPerformanceDto
} from '../../core/services/analytics.service';

Chart.register(DoughnutController, ArcElement, Tooltip, Legend);

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Reports</h1>
          <p class="page-subtitle">Explore spend distribution and vendor delivery performance.</p>
        </div>

        <div class="year-picker">
          <mat-icon>calendar_month</mat-icon>
          <select [(ngModel)]="selectedYear" (change)="onYearChange()">
            @for (year of yearOptions; track year) {
              <option [value]="year">{{ year }}</option>
            }
          </select>
        </div>
      </div>

      <div class="reports-grid">
        <div class="glass-card chart-card">
          <div class="section-header">
            <div>
              <h2 class="section-title">Spend by Category</h2>
              <p class="section-copy">Matched invoice spend grouped by item category proxy.</p>
            </div>
          </div>

          @if (loading()) {
            <div class="chart-placeholder">Loading chart…</div>
          } @else if (spendByCategory().length === 0) {
            <div class="chart-placeholder">No spend data for {{ selectedYear }}.</div>
          } @else {
            <div class="chart-wrapper">
              <canvas #spendChart></canvas>
            </div>

            <div class="legend-list">
              @for (item of spendByCategory(); track item.category) {
                <div class="legend-item">
                  <span class="legend-dot" [style.background]="getColor(item.category)"></span>
                  <div class="legend-copy">
                    <div class="legend-name">{{ item.category }}</div>
                    <div class="legend-meta">
                      {{ item.totalSpend | currency:item.currency:'symbol':'1.0-0' }}
                      · {{ percentage(item.totalSpend) | number:'1.0-1' }}%
                    </div>
                  </div>
                </div>
              }
            </div>
          }
        </div>

        <div class="glass-card table-card">
          <div class="section-header">
            <div>
              <h2 class="section-title">Vendor Performance</h2>
              <p class="section-copy">Sort by delivery reliability, match rate, or spend concentration.</p>
            </div>
          </div>

          @if (loading()) {
            <div class="chart-placeholder">Loading vendor metrics…</div>
          } @else if (sortedPerformance.length === 0) {
            <div class="chart-placeholder">No vendor performance data for {{ selectedYear }}.</div>
          } @else {
            <table class="data-table">
              <thead>
                <tr>
                  <th (click)="sortBy('vendorName')">Vendor</th>
                  <th (click)="sortBy('totalOrders')">Total Orders</th>
                  <th (click)="sortBy('onTimeDeliveryRate')">On-Time Rate</th>
                  <th (click)="sortBy('invoiceMatchRate')">Invoice Match Rate</th>
                  <th (click)="sortBy('totalSpend')">Total Spend</th>
                </tr>
              </thead>
              <tbody>
                @for (vendor of sortedPerformance; track vendor.vendorId) {
                  <tr>
                    <td>
                      <div class="vendor-name">{{ vendor.vendorName }}</div>
                    </td>
                    <td>{{ vendor.totalOrders }}</td>
                    <td>
                      <div class="progress-copy">{{ vendor.onTimeDeliveryRate | number:'1.0-1' }}%</div>
                      <div class="progress-track">
                        <div
                          class="progress-fill"
                          [class.good]="vendor.onTimeDeliveryRate >= 85"
                          [class.warn]="vendor.onTimeDeliveryRate >= 60 && vendor.onTimeDeliveryRate < 85"
                          [class.bad]="vendor.onTimeDeliveryRate < 60"
                          [style.width.%]="vendor.onTimeDeliveryRate">
                        </div>
                      </div>
                    </td>
                    <td>{{ vendor.invoiceMatchRate | number:'1.0-1' }}%</td>
                    <td>{{ vendor.totalSpend | currency:vendor.currency:'symbol':'1.0-0' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .page-header,
    .section-header {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: $space-lg;
    }

    .page-header { margin-bottom: $space-xl; }

    .year-picker {
      display: inline-flex;
      align-items: center;
      gap: $space-sm;
      padding: $space-sm $space-md;
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      background: rgba(255,255,255,0.04);
      color: $text-secondary;
      select {
        background: none;
        border: none;
        color: $text-primary;
        font-family: $font-body;
        outline: none;
      }
    }

    .reports-grid {
      display: grid;
      grid-template-columns: 1.1fr 1.4fr;
      gap: $space-lg;
    }

    .chart-card,
    .table-card {
      padding: $space-xl;
    }

    .section-title { color: $text-primary; font-size: $text-xl; margin-bottom: $space-xs; }
    .section-copy { color: $text-secondary; font-size: $text-sm; }

    .chart-wrapper {
      height: 280px;
      margin: $space-lg 0;
      position: relative;
    }

    .chart-placeholder {
      display: grid;
      place-items: center;
      min-height: 220px;
      color: $text-secondary;
      font-size: $text-sm;
    }

    .legend-list {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: $space-md;
    }

    .legend-item {
      display: flex;
      align-items: flex-start;
      gap: $space-sm;
      padding: $space-sm $space-md;
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      background: rgba(255,255,255,0.03);
    }

    .legend-dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-top: 5px;
      flex-shrink: 0;
    }

    .legend-name { color: $text-primary; font-size: $text-sm; font-weight: 600; }
    .legend-meta { color: $text-secondary; font-size: $text-xs; }

    .data-table { width: 100%; border-collapse: collapse; margin-top: $space-lg; }
    thead tr { border-bottom: 1px solid $border-subtle; }

    th, td {
      padding: $space-md 0;
      text-align: left;
      border-bottom: 1px solid rgba(255,255,255,0.04);
    }

    th {
      color: $text-muted;
      font-size: $text-xs;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      cursor: pointer;
      user-select: none;
    }

    td { color: $text-secondary; font-size: $text-sm; }
    .vendor-name { color: $text-primary; font-weight: 600; }

    .progress-copy {
      color: $text-primary;
      font-size: $text-xs;
      margin-bottom: 6px;
    }

    .progress-track {
      width: 140px;
      height: 8px;
      border-radius: $radius-pill;
      background: rgba(255,255,255,0.08);
      overflow: hidden;
    }

    .progress-fill {
      height: 100%;
      border-radius: inherit;
      background: $color-info;
      &.good { background: $color-accent; }
      &.warn { background: $color-warning; }
      &.bad { background: $color-danger; }
    }
  `]
})
export class ReportsComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('spendChart') spendChartRef?: ElementRef<HTMLCanvasElement>;

  spendByCategory = signal<SpendByCategoryDto[]>([]);
  vendorPerformance = signal<VendorPerformanceDto[]>([]);
  loading = signal(true);

  readonly currentYear = new Date().getFullYear();
  readonly yearOptions = [this.currentYear, this.currentYear - 1, this.currentYear - 2];

  selectedYear = this.currentYear;
  sortField: 'vendorName' | 'totalOrders' | 'onTimeDeliveryRate' | 'invoiceMatchRate' | 'totalSpend' = 'totalSpend';
  sortDirection: 'asc' | 'desc' = 'desc';

  private chart?: Chart;
  private viewReady = false;
  private readonly palette = ['#7c3aed', '#06b6d4', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#8b5cf6'];

  constructor(private analyticsService: AnalyticsService) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.renderChart();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  get sortedPerformance(): VendorPerformanceDto[] {
    const direction = this.sortDirection === 'asc' ? 1 : -1;
    return [...this.vendorPerformance()].sort((a, b) => {
      const left = a[this.sortField];
      const right = b[this.sortField];

      if (typeof left === 'string' && typeof right === 'string') {
        return left.localeCompare(right) * direction;
      }

      return (Number(left) - Number(right)) * direction;
    });
  }

  onYearChange(): void {
    this.loadData();
  }

  sortBy(field: typeof this.sortField): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
      return;
    }

    this.sortField = field;
    this.sortDirection = field === 'vendorName' ? 'asc' : 'desc';
  }

  percentage(amount: number): number {
    const total = this.totalSpend();
    return total === 0 ? 0 : (amount / total) * 100;
  }

  totalSpend(): number {
    return this.spendByCategory().reduce((sum, item) => sum + item.totalSpend, 0);
  }

  getColor(key: string): string {
    const index = this.spendByCategory().findIndex(item => item.category === key);
    return this.palette[index % this.palette.length];
  }

  private loadData(): void {
    this.loading.set(true);

    this.analyticsService.getSpendByCategory(this.selectedYear).subscribe({
      next: (spend) => {
        this.spendByCategory.set(spend);
        this.analyticsService.getVendorPerformance(this.selectedYear).subscribe({
          next: (performance) => {
            this.vendorPerformance.set(performance);
            this.loading.set(false);
            this.renderChart();
          },
          error: () => this.loading.set(false)
        });
      },
      error: () => this.loading.set(false)
    });
  }

  private renderChart(): void {
    if (!this.viewReady || !this.spendChartRef) {
      return;
    }

    const rows = this.spendByCategory();
    const ctx = this.spendChartRef.nativeElement.getContext('2d');
    if (!ctx) {
      return;
    }

    this.chart?.destroy();

    if (rows.length === 0) {
      return;
    }

    this.chart = new Chart(ctx, {
      type: 'doughnut',
      data: {
        labels: rows.map(item => item.category),
        datasets: [{
          data: rows.map(item => item.totalSpend),
          backgroundColor: rows.map(item => this.getColor(item.category)),
          borderWidth: 0,
          hoverOffset: 8
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
            borderWidth: 1
          }
        },
        cutout: '68%'
      }
    });
  }
}
