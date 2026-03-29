import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  CONTRACT_STATUS_CLASSES,
  CONTRACT_STATUS_LABELS,
  ContractDto,
  ContractsService
} from '../../core/services/contracts.service';
import { PaginatedList, VendorDto, VendorService } from '../../core/services/vendor.service';

@Component({
  selector: 'app-contracts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
    <div class="page-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">Contracts</h1>
          <p class="page-subtitle">Track supplier agreements, expiry risk, and renewal readiness.</p>
        </div>
        <button class="btn-primary" (click)="openCreatePanel(selectedVendorId || undefined)">
          <mat-icon>add</mat-icon>
          New Contract
        </button>
      </div>

      <div class="filter-bar glass-card">
        <div class="filter-chips">
          @for (filter of statusFilters; track filter.label) {
            <button
              class="chip"
              [class.active]="selectedStatus === filter.value"
              (click)="setStatusFilter(filter.value)">
              {{ filter.label }}
            </button>
          }
        </div>

        <select class="filter-select" [(ngModel)]="selectedVendorId" (change)="loadContracts()">
          <option value="">All Vendors</option>
          @for (vendor of vendors(); track vendor.id) {
            <option [value]="vendor.id">{{ vendor.companyName }}</option>
          }
        </select>

        <label class="toggle-pill">
          <input type="checkbox" [(ngModel)]="expiringOnly" (change)="loadContracts()" />
          <span>Expiring in 30 days</span>
        </label>
      </div>

      <div class="table-card glass-card">
        @if (isLoading()) {
          <div class="skeleton-wrapper">
            @for (item of [1, 2, 3, 4, 5]; track item) {
              <div class="skeleton-row"></div>
            }
          </div>
        } @else if (contracts().length === 0) {
          <div class="empty-state">
            <mat-icon>description</mat-icon>
            <h3>No contracts found</h3>
            <p>Create your first vendor contract or relax the current filters.</p>
          </div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>Contract Number</th>
                <th>Vendor</th>
                <th>Title</th>
                <th>Value</th>
                <th>Term</th>
                <th>Days Remaining</th>
                <th>Status</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (contract of contracts(); track contract.id) {
                <tr class="table-row">
                  <td>
                    <div class="primary-text">{{ contract.contractNumber }}</div>
                    <div class="secondary-text">{{ contract.createdAt | date:'mediumDate' }}</div>
                  </td>
                  <td>
                    <a [routerLink]="['/vendors', contract.vendorId]" class="vendor-link">{{ contract.vendorName }}</a>
                  </td>
                  <td>{{ contract.title }}</td>
                  <td class="value-cell">
                    {{ contract.value | currency:contract.currency:'symbol':'1.0-0' }}
                  </td>
                  <td>
                    <div class="primary-text">{{ contract.startDate | date:'mediumDate' }}</div>
                    <div class="secondary-text">{{ contract.endDate | date:'mediumDate' }}</div>
                  </td>
                  <td>
                    <span class="days-pill" [class.urgent]="contract.daysRemaining <= 30">
                      {{ contract.daysRemaining }} days
                    </span>
                  </td>
                  <td>
                    <span class="status-badge" [ngClass]="getStatusClass(contract.status)">
                      {{ getStatusLabel(contract.status) }}
                    </span>
                  </td>
                  <td>
                    <button class="link-btn" [routerLink]="['/vendors', contract.vendorId]">Open Vendor</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>

          @if (pagination(); as pg) {
            <div class="pagination">
              <span class="pagination-info">
                Showing {{ (pg.pageNumber - 1) * pageSize + 1 }}–{{ Math.min(pg.pageNumber * pageSize, pg.totalCount) }}
                of {{ pg.totalCount }} contracts
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

    @if (showCreatePanel()) {
      <div class="drawer-overlay" (click)="closeCreatePanel()"></div>
      <div class="drawer glass-card">
        <div class="drawer-header">
          <div>
            <h2 class="drawer-title">New Contract</h2>
            <p class="drawer-subtitle">Create a supplier agreement with renewal visibility built in.</p>
          </div>
          <button class="close-btn" type="button" (click)="closeCreatePanel()">
            <mat-icon>close</mat-icon>
          </button>
        </div>

        @if (createError()) {
          <div class="error-banner">
            <mat-icon>error_outline</mat-icon>
            {{ createError() }}
          </div>
        }

        <form class="drawer-form" [formGroup]="createForm" (ngSubmit)="submitCreate()">
          <div class="field-group">
            <label class="field-label">Vendor *</label>
            <select class="field-input" formControlName="vendorId">
              <option value="">Select an approved vendor</option>
              @for (vendor of vendors(); track vendor.id) {
                <option [value]="vendor.id">{{ vendor.companyName }}</option>
              }
            </select>
          </div>

          <div class="field-group">
            <label class="field-label">Title *</label>
            <input class="field-input" formControlName="title" placeholder="e.g. Regional Freight Services 2026" />
          </div>

          <div class="form-row">
            <div class="field-group">
              <label class="field-label">Start Date *</label>
              <input class="field-input" type="date" formControlName="startDate" />
            </div>
            <div class="field-group">
              <label class="field-label">End Date *</label>
              <input class="field-input" type="date" formControlName="endDate" />
            </div>
          </div>

          <div class="form-row">
            <div class="field-group">
              <label class="field-label">Value *</label>
              <input class="field-input" type="number" min="0.01" step="0.01" formControlName="value" />
            </div>
            <div class="field-group currency-group">
              <label class="field-label">Currency *</label>
              <input class="field-input" formControlName="currency" maxlength="3" />
            </div>
          </div>

          <div class="drawer-actions">
            <button class="btn-secondary" type="button" (click)="closeCreatePanel()">Cancel</button>
            <button class="btn-primary" type="submit" [disabled]="isSubmitting()">
              @if (isSubmitting()) {
                <span class="btn-spinner"></span>
                Creating...
              } @else {
                <mat-icon>save</mat-icon>
                Create Contract
              }
            </button>
          </div>
        </form>
      </div>
    }
  `,
  styles: [`
    @use 'styles/variables' as *;

    .page-header,
    .drawer-header,
    .pagination,
    .drawer-actions,
    .form-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: $space-lg;
    }

    .page-header {
      align-items: flex-start;
      margin-bottom: $space-xl;
    }

    .btn-primary,
    .btn-secondary {
      display: inline-flex;
      align-items: center;
      gap: $space-sm;
      padding: $space-sm $space-lg;
      border-radius: $radius-md;
      border: none;
      cursor: pointer;
      transition: all $transition-fast;
      font-size: $text-sm;
      font-weight: 600;
    }

    .btn-primary {
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 10%));
      color: white;
      &:hover { transform: translateY(-1px); box-shadow: $shadow-glow; }
    }

    .btn-secondary {
      background: transparent;
      color: $text-secondary;
      border: 1px solid $border-subtle;
      &:hover { color: $text-primary; border-color: $border-light; }
    }

    .filter-bar {
      display: flex;
      align-items: center;
      gap: $space-md;
      padding: $space-md $space-lg;
      margin-bottom: $space-lg;
      flex-wrap: wrap;
    }

    .filter-chips { display: flex; gap: $space-xs; flex-wrap: wrap; }

    .chip {
      padding: 6px $space-md;
      border-radius: $radius-pill;
      border: 1px solid $border-subtle;
      background: transparent;
      color: $text-secondary;
      cursor: pointer;
      font-size: $text-xs;
      transition: all $transition-fast;
      &:hover { border-color: $color-primary; color: $color-primary; }
      &.active { color: $color-primary; border-color: $color-primary; background: rgba($color-primary, 0.12); }
    }

    .filter-select,
    .field-input {
      background: rgba(255,255,255,0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      color: $text-primary;
      font-size: $text-sm;
      padding: $space-sm $space-md;
      outline: none;
      font-family: $font-body;
      option { background: $bg-secondary; }
    }

    .toggle-pill {
      display: inline-flex;
      align-items: center;
      gap: $space-sm;
      color: $text-secondary;
      font-size: $text-sm;
      margin-left: auto;
      input { accent-color: $color-primary; }
    }

    .table-card { overflow: hidden; }
    .data-table { width: 100%; border-collapse: collapse; }
    thead tr { border-bottom: 1px solid $border-subtle; }

    th, td {
      padding: $space-md $space-lg;
      text-align: left;
      vertical-align: middle;
    }

    th {
      color: $text-muted;
      font-size: $text-xs;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .table-row {
      border-bottom: 1px solid $border-subtle;
      transition: background $transition-fast;
      &:hover { background: rgba(255,255,255,0.03); }
      &:last-child { border-bottom: none; }
    }

    .primary-text { color: $text-primary; font-size: $text-sm; font-weight: 600; }
    .secondary-text { color: $text-secondary; font-size: $text-xs; margin-top: 2px; }
    .vendor-link, .link-btn { color: $color-primary; text-decoration: none; }
    .link-btn { background: none; border: none; cursor: pointer; font-size: $text-sm; }
    .value-cell { font-weight: 700; color: $text-primary; }

    .days-pill {
      display: inline-flex;
      align-items: center;
      padding: 4px $space-sm;
      border-radius: $radius-pill;
      background: rgba($color-accent, 0.15);
      color: $color-accent;
      font-size: $text-xs;
      font-weight: 700;
      &.urgent {
        background: rgba($color-warning, 0.15);
        color: $color-warning;
      }
    }

    .status-active { background: rgba($color-accent, 0.15); color: $color-accent; }
    .status-warning { background: rgba($color-warning, 0.15); color: $color-warning; }
    .status-danger { background: rgba($color-danger, 0.15); color: $color-danger; }
    .status-muted { background: rgba(255,255,255,0.08); color: $text-secondary; }
    .status-draft { background: rgba($color-info, 0.15); color: $color-info; }
    .status-info { background: rgba($color-primary, 0.15); color: $color-primary; }

    .pagination {
      padding: $space-md $space-lg;
      border-top: 1px solid $border-subtle;
    }

    .pagination-info,
    .page-indicator { color: $text-secondary; font-size: $text-sm; }
    .pagination-controls { display: flex; align-items: center; gap: $space-sm; }

    .page-btn {
      width: 32px;
      height: 32px;
      border-radius: $radius-md;
      border: 1px solid $border-subtle;
      background: none;
      color: $text-secondary;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
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

    .drawer-overlay {
      position: fixed;
      inset: 0;
      background: rgba(0,0,0,0.5);
      backdrop-filter: blur(4px);
      z-index: 200;
    }

    .drawer {
      position: fixed;
      top: 0;
      right: 0;
      bottom: 0;
      width: min(520px, 100vw);
      z-index: 201;
      overflow-y: auto;
      border-radius: $radius-lg 0 0 $radius-lg;
    }

    .drawer-header {
      align-items: flex-start;
      padding: $space-xl;
      border-bottom: 1px solid $border-subtle;
      position: sticky;
      top: 0;
      background: inherit;
      backdrop-filter: blur(18px);
    }

    .drawer-title { color: $text-primary; font-size: $text-2xl; margin-bottom: $space-xs; }
    .drawer-subtitle { color: $text-secondary; font-size: $text-sm; }

    .close-btn {
      background: none;
      border: none;
      color: $text-muted;
      cursor: pointer;
      display: inline-flex;
      padding: 4px;
      &:hover { color: $text-primary; }
    }

    .drawer-form { padding: $space-xl; }
    .field-group { display: flex; flex-direction: column; gap: $space-xs; margin-bottom: $space-lg; flex: 1; }
    .field-label { color: $text-secondary; font-size: $text-sm; }
    .currency-group { max-width: 140px; }

    .error-banner {
      display: flex;
      align-items: center;
      gap: $space-sm;
      padding: $space-md $space-xl 0;
      color: $color-danger;
      font-size: $text-sm;
    }

    .btn-spinner {
      width: 16px;
      height: 16px;
      border-radius: 50%;
      border: 2px solid rgba(255,255,255,0.35);
      border-top-color: white;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin { to { transform: rotate(360deg); } }
    @keyframes shimmer { 0% { background-position: -200% 0; } 100% { background-position: 200% 0; } }
  `]
})
export class ContractsComponent implements OnInit {
  contracts = signal<ContractDto[]>([]);
  vendors = signal<VendorDto[]>([]);
  pagination = signal<PaginatedList<ContractDto> | null>(null);
  isLoading = signal(true);
  isSubmitting = signal(false);
  showCreatePanel = signal(false);
  createError = signal('');

  currentPage = 1;
  pageSize = 10;
  selectedStatus: number | undefined = undefined;
  selectedVendorId = '';
  expiringOnly = false;

  readonly Math = Math;
  readonly statusFilters = [
    { label: 'All', value: undefined as number | undefined },
    { label: 'Active', value: 1 },
    { label: 'Expiring', value: 2 },
    { label: 'Expired', value: 3 },
    { label: 'Terminated', value: 4 }
  ];

  createForm = this.fb.nonNullable.group({
    vendorId: ['', Validators.required],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
    value: [0, [Validators.required, Validators.min(0.01)]],
    currency: ['USD', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]]
  });

  constructor(
    private contractsService: ContractsService,
    private vendorService: VendorService,
    private route: ActivatedRoute,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    const vendorId = this.route.snapshot.queryParamMap.get('vendorId');
    const open = this.route.snapshot.queryParamMap.get('open') === 'true';

    if (vendorId) {
      this.selectedVendorId = vendorId;
      this.createForm.patchValue({ vendorId });
    }

    if (open) {
      this.showCreatePanel.set(true);
    }

    this.loadVendors();
    this.loadContracts();
  }

  loadContracts(): void {
    this.isLoading.set(true);
    this.contractsService.getAll(this.currentPage, this.pageSize, {
      status: this.selectedStatus,
      vendorId: this.selectedVendorId || undefined,
      expiringWithin30Days: this.expiringOnly
    }).subscribe({
      next: (result) => {
        this.contracts.set(result.items);
        this.pagination.set(result);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadVendors(): void {
    this.vendorService.getVendors(1, 100, undefined, 1).subscribe({
      next: (result) => this.vendors.set(result.items),
      error: () => this.vendors.set([])
    });
  }

  openCreatePanel(vendorId?: string): void {
    if (vendorId) {
      this.createForm.patchValue({ vendorId });
    }

    this.showCreatePanel.set(true);
  }

  closeCreatePanel(): void {
    this.showCreatePanel.set(false);
    this.isSubmitting.set(false);
    this.createError.set('');
    this.createForm.patchValue({
      vendorId: this.selectedVendorId || '',
      title: '',
      startDate: '',
      endDate: '',
      value: 0,
      currency: 'USD'
    });
    this.createForm.markAsPristine();
    this.createForm.markAsUntouched();
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.createError.set('');

    const payload = this.createForm.getRawValue();
    this.contractsService.create({
      vendorId: payload.vendorId,
      title: payload.title.trim(),
      startDate: payload.startDate,
      endDate: payload.endDate,
      value: Number(payload.value),
      currency: payload.currency.toUpperCase()
    }).subscribe({
      next: () => {
        this.closeCreatePanel();
        this.currentPage = 1;
        this.loadContracts();
      },
      error: (error) => {
        this.isSubmitting.set(false);
        this.createError.set(error?.error?.error ?? 'Unable to create contract right now.');
      }
    });
  }

  setStatusFilter(status: number | undefined): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadContracts();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadContracts();
  }

  getStatusLabel(status: number): string {
    return CONTRACT_STATUS_LABELS[status] ?? 'Unknown';
  }

  getStatusClass(status: number): string {
    return CONTRACT_STATUS_CLASSES[status] ?? 'status-muted';
  }
}
