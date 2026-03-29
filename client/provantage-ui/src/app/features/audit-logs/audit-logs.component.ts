import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { AuditLogDto, AuditLogService } from '../../core/services/audit-log.service';
import { PaginatedList } from '../../core/services/vendor.service';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Audit Logs</h1>
          <p class="page-subtitle">Review tenant activity, record changes, and operational history.</p>
        </div>
      </div>

      <div class="filter-bar glass-card">
        <select class="filter-input" [(ngModel)]="selectedEntityType" (change)="applyFilters()">
          <option value="">All Entities</option>
          @for (type of entityTypes; track type) {
            <option [value]="type">{{ type }}</option>
          }
        </select>

        <input class="filter-input" type="date" [(ngModel)]="fromDate" (change)="applyFilters()" />
        <input class="filter-input" type="date" [(ngModel)]="toDate" (change)="applyFilters()" />

        <button class="reset-btn" type="button" (click)="resetFilters()">
          <mat-icon>restart_alt</mat-icon>
          Reset
        </button>
      </div>

      <div class="table-card glass-card">
        @if (isLoading()) {
          <div class="skeleton-wrapper">
            @for (item of [1, 2, 3, 4, 5]; track item) {
              <div class="skeleton-row"></div>
            }
          </div>
        } @else if (logs().length === 0) {
          <div class="empty-state">
            <mat-icon>history</mat-icon>
            <h3>No audit entries found</h3>
            <p>Adjust the filters or perform a few actions in the app to populate this view.</p>
          </div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Action</th>
                <th>Entity Type</th>
                <th>Entity ID</th>
                <th>Performed By</th>
                <th>Changes</th>
              </tr>
            </thead>
            <tbody>
              @for (log of logs(); track log.id) {
                <tr class="table-row" (click)="toggleExpanded(log.id)">
                  <td>{{ log.timestamp | date:'medium' }}</td>
                  <td>
                    <span class="action-badge" [ngClass]="getActionClass(log.action)">{{ log.action }}</span>
                  </td>
                  <td>{{ log.entityType }}</td>
                  <td class="entity-id">{{ log.entityId }}</td>
                  <td>{{ log.performedBy }}</td>
                  <td>
                    <button class="link-btn" type="button">
                      {{ expandedId() === log.id ? 'Hide' : 'View' }}
                    </button>
                  </td>
                </tr>

                @if (expandedId() === log.id) {
                  <tr class="expanded-row">
                    <td colspan="6">
                      <div class="diff-grid">
                        <div class="diff-card">
                          <div class="diff-title">Old Values</div>
                          <pre>{{ formatJson(log.oldValues) }}</pre>
                        </div>
                        <div class="diff-card">
                          <div class="diff-title">New Values</div>
                          <pre>{{ formatJson(log.newValues) }}</pre>
                        </div>
                      </div>
                    </td>
                  </tr>
                }
              }
            </tbody>
          </table>

          @if (pagination(); as pg) {
            <div class="pagination">
              <span class="pagination-info">
                Showing {{ (pg.pageNumber - 1) * pageSize + 1 }}–{{ Math.min(pg.pageNumber * pageSize, pg.totalCount) }}
                of {{ pg.totalCount }} log entries
              </span>
              <div class="pagination-controls">
                <button class="page-btn" [disabled]="!pg.hasPreviousPage" (click)="goToPage(currentPage - 1)">
                  <mat-icon>chevron_left</mat-icon>
                </button>
                <span class="page-indicator">{{ pg.pageNumber }} / {{ pg.totalPages }}</span>
                <button class="page-btn" [disabled]="!pg.hasNextPage" (click)="goToPage(currentPage + 1)">
                  <mat-icon>chevron_right</mat-icon>
                </button>
              </div>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .page-header { margin-bottom: $space-xl; }

    .filter-bar,
    .pagination,
    .pagination-controls,
    .diff-grid {
      display: flex;
      gap: $space-md;
      align-items: center;
    }

    .filter-bar {
      flex-wrap: wrap;
      padding: $space-md $space-lg;
      margin-bottom: $space-lg;
    }

    .filter-input {
      min-width: 180px;
      background: rgba(255,255,255,0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      color: $text-primary;
      padding: $space-sm $space-md;
      font-family: $font-body;
      option { background: $bg-secondary; }
    }

    .reset-btn,
    .link-btn,
    .page-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: $space-xs;
      cursor: pointer;
      transition: all $transition-fast;
    }

    .reset-btn {
      margin-left: auto;
      border: 1px solid $border-subtle;
      background: none;
      color: $text-secondary;
      border-radius: $radius-md;
      padding: $space-sm $space-md;
      &:hover { color: $text-primary; border-color: $border-light; }
    }

    .table-card { overflow: hidden; }
    .data-table { width: 100%; border-collapse: collapse; }
    thead tr { border-bottom: 1px solid $border-subtle; }

    th, td {
      padding: $space-md $space-lg;
      text-align: left;
      vertical-align: top;
    }

    th {
      color: $text-muted;
      font-size: $text-xs;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .table-row {
      border-bottom: 1px solid $border-subtle;
      cursor: pointer;
      &:hover { background: rgba(255,255,255,0.03); }
    }

    .entity-id {
      font-family: 'SFMono-Regular', Consolas, monospace;
      color: $text-secondary;
      font-size: $text-xs;
    }

    .action-badge {
      display: inline-flex;
      align-items: center;
      padding: 4px $space-sm;
      border-radius: $radius-pill;
      font-size: $text-xs;
      font-weight: 700;
    }

    .action-created { background: rgba($color-accent, 0.15); color: $color-accent; }
    .action-updated { background: rgba($color-info, 0.15); color: $color-info; }
    .action-deleted { background: rgba($color-danger, 0.15); color: $color-danger; }
    .action-matched,
    .action-approved,
    .action-submitted { background: rgba($color-primary, 0.15); color: $color-primary; }

    .link-btn {
      background: none;
      border: none;
      color: $color-primary;
      font-size: $text-sm;
      justify-content: flex-start;
    }

    .expanded-row td {
      background: rgba(255,255,255,0.02);
      border-bottom: 1px solid $border-subtle;
    }

    .diff-grid {
      align-items: stretch;
      gap: $space-lg;
    }

    .diff-card {
      flex: 1;
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      padding: $space-md;
      background: rgba(255,255,255,0.03);
    }

    .diff-title {
      color: $text-primary;
      font-size: $text-sm;
      font-weight: 600;
      margin-bottom: $space-sm;
    }

    pre {
      margin: 0;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: 'SFMono-Regular', Consolas, monospace;
      color: $text-secondary;
      font-size: 12px;
      line-height: 1.5;
    }

    .pagination {
      justify-content: space-between;
      padding: $space-md $space-lg;
      border-top: 1px solid $border-subtle;
    }

    .pagination-info,
    .page-indicator { color: $text-secondary; font-size: $text-sm; }

    .page-btn {
      width: 32px;
      height: 32px;
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      background: none;
      color: $text-secondary;
      &:hover:not(:disabled) { border-color: $color-primary; color: $color-primary; }
      &:disabled { opacity: 0.3; cursor: not-allowed; }
    }

    .skeleton-wrapper { padding: $space-lg; }
    .skeleton-row {
      height: 56px;
      border-radius: $radius-md;
      margin-bottom: $space-sm;
      background: linear-gradient(90deg, rgba(255,255,255,0.04) 25%, rgba(255,255,255,0.08) 50%, rgba(255,255,255,0.04) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
    }

    .empty-state {
      text-align: center;
      padding: $space-3xl;
      color: $text-secondary;
      mat-icon { font-size: 48px; width: 48px; height: 48px; margin-bottom: $space-md; color: $text-muted; }
      h3 { color: $text-primary; margin-bottom: $space-sm; }
    }

    @keyframes shimmer { 0% { background-position: -200% 0; } 100% { background-position: 200% 0; } }
  `]
})
export class AuditLogsComponent implements OnInit {
  logs = signal<AuditLogDto[]>([]);
  pagination = signal<PaginatedList<AuditLogDto> | null>(null);
  isLoading = signal(true);
  expandedId = signal<string | null>(null);

  readonly Math = Math;
  readonly entityTypes = ['Vendor', 'Invoice', 'PurchaseOrder', 'PurchaseRequisition', 'Contract', 'BudgetAllocation', 'Notification'];

  selectedEntityType = '';
  fromDate = '';
  toDate = '';
  currentPage = 1;
  pageSize = 20;

  constructor(private auditLogService: AuditLogService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading.set(true);
    this.auditLogService.getAll({
      entityType: this.selectedEntityType || undefined,
      from: this.fromDate || undefined,
      to: this.toDate || undefined,
      page: this.currentPage,
      pageSize: this.pageSize
    }).subscribe({
      next: (result) => {
        this.logs.set(result.items);
        this.pagination.set(result);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.expandedId.set(null);
    this.loadLogs();
  }

  resetFilters(): void {
    this.selectedEntityType = '';
    this.fromDate = '';
    this.toDate = '';
    this.applyFilters();
  }

  toggleExpanded(id: string): void {
    this.expandedId.update(current => current === id ? null : id);
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadLogs();
  }

  getActionClass(action: string): string {
    return `action-${action.toLowerCase()}`;
  }

  formatJson(value: string | null): string {
    if (!value) {
      return 'No data captured.';
    }

    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }
}
