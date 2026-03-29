import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  VendorService,
  VendorDto,
  PaginatedList,
  VENDOR_STATUS_LABELS,
  VENDOR_STATUS_CLASSES
} from '../../core/services/vendor.service';

@Component({
  selector: 'app-vendors',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatIconModule, MatMenuModule, MatButtonModule, MatTooltipModule],
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Vendors</h1>
          <p class="page-subtitle">Manage your supplier relationships and approvals</p>
        </div>
        <button class="btn-primary" (click)="showCreatePanel.set(true)">
          <mat-icon>add</mat-icon>
          Add Vendor
        </button>
      </div>

      <!-- Filters Bar -->
      <div class="filter-bar glass-card">
        <div class="search-wrapper">
          <mat-icon>search</mat-icon>
          <input
            type="text"
            class="search-input"
            placeholder="Search vendors..."
            [(ngModel)]="searchTerm"
            (input)="onSearch()" />
          @if (searchTerm) {
            <button class="clear-btn" (click)="clearSearch()">
              <mat-icon>close</mat-icon>
            </button>
          }
        </div>

        <div class="filter-chips">
          @for (filter of statusFilters; track filter.value) {
            <button
              class="chip"
              [class.active]="selectedStatus === filter.value"
              (click)="setStatusFilter(filter.value)">
              {{ filter.label }}
            </button>
          }
        </div>

        <select class="filter-select" [(ngModel)]="selectedCategory" (change)="loadVendors()">
          <option value="">All Categories</option>
          @for (cat of categories(); track cat) {
            <option [value]="cat">{{ cat }}</option>
          }
        </select>
      </div>

      <!-- Table -->
      <div class="table-card glass-card">
        @if (isLoading()) {
          <div class="skeleton-wrapper">
            @for (i of [1,2,3,4,5]; track i) {
              <div class="skeleton-row"></div>
            }
          </div>
        } @else if (vendors().length === 0) {
          <div class="empty-state">
            <mat-icon>store_mall_directory</mat-icon>
            <h3>No vendors found</h3>
            <p>{{ searchTerm ? 'Try a different search term.' : 'Add your first vendor to get started.' }}</p>
          </div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>Company</th>
                <th>Category</th>
                <th>Contact</th>
                <th>Status</th>
                <th>Rating</th>
                <th>Location</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (vendor of vendors(); track vendor.id) {
                <tr class="table-row" (click)="viewVendor(vendor.id)">
                  <td>
                    <div class="vendor-name">
                      <div class="vendor-avatar">{{ getInitials(vendor.companyName) }}</div>
                      <div>
                        <div class="primary-text">{{ vendor.companyName }}</div>
                        <div class="secondary-text">{{ vendor.paymentTerms }}</div>
                      </div>
                    </div>
                  </td>
                  <td>
                    <span class="category-badge">{{ vendor.category }}</span>
                  </td>
                  <td>
                    <div class="primary-text">{{ vendor.email }}</div>
                    <div class="secondary-text">{{ vendor.phone }}</div>
                  </td>
                  <td>
                    <span class="status-badge" [class]="getStatusClass(vendor.status)">
                      {{ getStatusLabel(vendor.status) }}
                    </span>
                  </td>
                  <td>
                    <div class="rating">
                      <mat-icon class="star-icon">star</mat-icon>
                      {{ vendor.rating.toFixed(1) }}
                    </div>
                  </td>
                  <td class="secondary-text">{{ vendor.city }}, {{ vendor.country }}</td>
                  <td (click)="$event.stopPropagation()">
                    <button mat-icon-button [matMenuTriggerFor]="menu" class="action-btn">
                      <mat-icon>more_vert</mat-icon>
                    </button>
                    <mat-menu #menu="matMenu">
                      <button mat-menu-item (click)="viewVendor(vendor.id)">
                        <mat-icon>visibility</mat-icon> View Details
                      </button>
                      @if (vendor.status === 0) {
                        <button mat-menu-item (click)="changeStatus(vendor.id, 1)">
                          <mat-icon>check_circle</mat-icon> Approve
                        </button>
                      }
                      @if (vendor.status === 1) {
                        <button mat-menu-item (click)="changeStatus(vendor.id, 2)">
                          <mat-icon>pause_circle</mat-icon> Suspend
                        </button>
                      }
                      @if (vendor.status !== 3) {
                        <button mat-menu-item class="danger-item" (click)="changeStatus(vendor.id, 3)">
                          <mat-icon>block</mat-icon> Blacklist
                        </button>
                      }
                    </mat-menu>
                  </td>
                </tr>
              }
            </tbody>
          </table>

          <!-- Pagination -->
          @if (pagination(); as pg) {
            <div class="pagination">
              <span class="pagination-info">
                Showing {{ (pg.pageNumber - 1) * pageSize + 1 }}–{{ Math.min(pg.pageNumber * pageSize, pg.totalCount) }}
                of {{ pg.totalCount }} vendors
              </span>
              <div class="pagination-controls">
                <button
                  class="page-btn"
                  [disabled]="!pg.hasPreviousPage"
                  (click)="goToPage(currentPage - 1)">
                  <mat-icon>chevron_left</mat-icon>
                </button>
                <span class="page-indicator">{{ pg.pageNumber }} / {{ pg.totalPages }}</span>
                <button
                  class="page-btn"
                  [disabled]="!pg.hasNextPage"
                  (click)="goToPage(currentPage + 1)">
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

    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: $space-xl;
    }

    .btn-primary {
      display: flex;
      align-items: center;
      gap: $space-sm;
      padding: $space-sm $space-lg;
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 10%));
      color: white;
      border: none;
      border-radius: $radius-md;
      font-size: $text-sm;
      font-weight: 600;
      cursor: pointer;
      transition: all $transition-fast;

      &:hover { transform: translateY(-1px); box-shadow: $shadow-glow; }

      mat-icon { font-size: 20px; width: 20px; height: 20px; }
    }

    .filter-bar {
      display: flex;
      align-items: center;
      gap: $space-lg;
      padding: $space-md $space-lg;
      margin-bottom: $space-lg;
      flex-wrap: wrap;
    }

    .search-wrapper {
      display: flex;
      align-items: center;
      gap: $space-sm;
      background: rgba(255,255,255,0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      padding: $space-sm $space-md;
      flex: 1;
      min-width: 200px;

      mat-icon { color: $text-muted; font-size: 18px; width: 18px; height: 18px; }
    }

    .search-input {
      background: none;
      border: none;
      outline: none;
      color: $text-primary;
      font-size: $text-sm;
      flex: 1;
      &::placeholder { color: $text-muted; }
    }

    .clear-btn {
      background: none;
      border: none;
      cursor: pointer;
      color: $text-muted;
      padding: 0;
      display: flex;
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
    }

    .filter-chips {
      display: flex;
      gap: $space-xs;
    }

    .chip {
      padding: 6px $space-md;
      border-radius: $radius-pill;
      border: 1px solid $border-subtle;
      background: transparent;
      color: $text-secondary;
      font-size: $text-xs;
      font-weight: 500;
      cursor: pointer;
      transition: all $transition-fast;

      &:hover { border-color: $color-primary; color: $color-primary; }
      &.active { background: rgba($color-primary, 0.12); border-color: $color-primary; color: $color-primary; }
    }

    .filter-select {
      background: rgba(255,255,255,0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      color: $text-secondary;
      font-size: $text-sm;
      padding: $space-sm $space-md;
      outline: none;
      cursor: pointer;

      option { background: $bg-secondary; }
    }

    .table-card {
      overflow: hidden;
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;
    }

    thead tr {
      border-bottom: 1px solid $border-subtle;
    }

    th {
      padding: $space-md $space-lg;
      text-align: left;
      font-size: $text-xs;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: $text-muted;
    }

    .table-row {
      border-bottom: 1px solid $border-subtle;
      cursor: pointer;
      transition: background $transition-fast;

      &:hover { background: rgba(255,255,255,0.03); }
      &:last-child { border-bottom: none; }
    }

    td {
      padding: $space-md $space-lg;
      vertical-align: middle;
    }

    .vendor-name {
      display: flex;
      align-items: center;
      gap: $space-md;
    }

    .vendor-avatar {
      width: 36px;
      height: 36px;
      border-radius: $radius-sm;
      background: linear-gradient(135deg, $color-primary, $color-accent);
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: $text-xs;
      color: white;
      flex-shrink: 0;
    }

    .primary-text {
      font-size: $text-sm;
      font-weight: 500;
      color: $text-primary;
    }

    .secondary-text {
      font-size: $text-xs;
      color: $text-secondary;
      margin-top: 2px;
    }

    .category-badge {
      padding: 4px $space-sm;
      background: rgba($color-info, 0.1);
      color: $color-info;
      border-radius: $radius-pill;
      font-size: $text-xs;
      font-weight: 500;
    }

    .status-badge {
      padding: 4px $space-sm;
      border-radius: $radius-pill;
      font-size: $text-xs;
      font-weight: 600;
    }

    .status-pending  { background: rgba($color-warning, 0.1); color: $color-warning; }
    .status-approved { background: rgba($color-accent, 0.1);  color: $color-accent;  }
    .status-warning  { background: rgba($color-warning, 0.1); color: $color-warning; }
    .status-danger   { background: rgba($color-danger, 0.1);  color: $color-danger;  }

    .rating {
      display: flex;
      align-items: center;
      gap: 4px;
      color: $color-warning;
      font-size: $text-sm;
      font-weight: 600;
    }

    .star-icon { font-size: 16px; width: 16px; height: 16px; }

    .action-btn { color: $text-secondary; }

    ::ng-deep .danger-item { color: $color-danger !important; }
    ::ng-deep .danger-item mat-icon { color: $color-danger !important; }

    .skeleton-wrapper { padding: $space-lg; }
    .skeleton-row {
      height: 56px;
      background: linear-gradient(90deg, rgba(255,255,255,0.04) 25%, rgba(255,255,255,0.08) 50%, rgba(255,255,255,0.04) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: $radius-md;
      margin-bottom: $space-sm;
    }

    @keyframes shimmer {
      0% { background-position: -200% 0; }
      100% { background-position: 200% 0; }
    }

    .empty-state {
      text-align: center;
      padding: $space-3xl;
      color: $text-secondary;

      mat-icon { font-size: 48px; width: 48px; height: 48px; color: $text-muted; margin-bottom: $space-md; }
      h3 { font-size: $text-xl; color: $text-primary; margin-bottom: $space-sm; }
      p { font-size: $text-sm; }
    }

    .pagination {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: $space-md $space-lg;
      border-top: 1px solid $border-subtle;
    }

    .pagination-info { font-size: $text-sm; color: $text-secondary; }

    .pagination-controls {
      display: flex;
      align-items: center;
      gap: $space-sm;
    }

    .page-btn {
      width: 32px;
      height: 32px;
      border-radius: $radius-md;
      border: 1px solid $border-subtle;
      background: none;
      color: $text-secondary;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all $transition-fast;

      &:hover:not(:disabled) { border-color: $color-primary; color: $color-primary; }
      &:disabled { opacity: 0.3; cursor: not-allowed; }

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .page-indicator { font-size: $text-sm; color: $text-secondary; padding: 0 $space-sm; }
  `]
})
export class VendorsComponent implements OnInit {
  vendors = signal<VendorDto[]>([]);
  pagination = signal<PaginatedList<VendorDto> | null>(null);
  categories = signal<string[]>([]);
  isLoading = signal(true);
  showCreatePanel = signal(false);

  searchTerm = '';
  selectedStatus: number | undefined = undefined;
  selectedCategory = '';
  currentPage = 1;
  pageSize = 10;

  readonly Math = Math;

  statusFilters = [
    { label: 'All', value: undefined as number | undefined },
    { label: 'Pending', value: 0 },
    { label: 'Approved', value: 1 },
    { label: 'Suspended', value: 2 },
    { label: 'Blacklisted', value: 3 },
  ];

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private vendorService: VendorService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadVendors();
    this.vendorService.getCategories().subscribe(cats => this.categories.set(cats));
  }

  loadVendors(): void {
    this.isLoading.set(true);
    this.vendorService
      .getVendors(this.currentPage, this.pageSize, this.searchTerm || undefined, this.selectedStatus, this.selectedCategory || undefined)
      .subscribe({
        next: (data) => {
          this.vendors.set(data.items);
          this.pagination.set(data);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      });
  }

  onSearch(): void {
    if (this.searchDebounce) clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.currentPage = 1;
      this.loadVendors();
    }, 350);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.currentPage = 1;
    this.loadVendors();
  }

  setStatusFilter(status: number | undefined): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadVendors();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadVendors();
  }

  viewVendor(id: string): void {
    this.router.navigate(['/vendors', id]);
  }

  changeStatus(id: string, status: number): void {
    this.vendorService.changeStatus(id, status).subscribe(() => this.loadVendors());
  }

  getInitials(name: string): string {
    return name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase();
  }

  getStatusLabel(status: number): string {
    return VENDOR_STATUS_LABELS[status] ?? 'Unknown';
  }

  getStatusClass(status: number): string {
    return VENDOR_STATUS_CLASSES[status] ?? '';
  }
}
