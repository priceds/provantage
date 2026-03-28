import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import {
  RequisitionService,
  RequisitionDto,
  CreateRequisitionRequest,
  REQUISITION_STATUS_LABELS,
  REQUISITION_STATUS_CLASSES
} from '../../core/services/requisition.service';
import { PaginatedList } from '../../core/services/vendor.service';

@Component({
  selector: 'app-requisitions',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatIconModule, MatMenuModule, MatButtonModule, MatDialogModule
  ],
  template: `
    <div class="page-container">
      <!-- Page Header -->
      <div class="page-header">
        <div>
          <h1 class="page-title">Purchase Requisitions</h1>
          <p class="page-subtitle">Manage procurement requests and approvals</p>
        </div>
        <button class="btn-primary" (click)="openCreateForm()">
          <mat-icon>add</mat-icon>
          New Requisition
        </button>
      </div>

      <!-- Filters -->
      <div class="filter-bar glass-card">
        <div class="search-wrapper">
          <mat-icon>search</mat-icon>
          <input
            type="text"
            class="search-input"
            placeholder="Search requisitions..."
            [(ngModel)]="searchTerm"
            (input)="onSearch()" />
          @if (searchTerm) {
            <button class="clear-btn" (click)="clearSearch()">
              <mat-icon>close</mat-icon>
            </button>
          }
        </div>

        <div class="filter-chips">
          @for (f of statusFilters; track f.label) {
            <button
              class="chip"
              [class.active]="selectedStatus === f.value"
              (click)="setStatusFilter(f.value)">
              {{ f.label }}
            </button>
          }
        </div>
      </div>

      <!-- Table -->
      <div class="table-card glass-card">
        @if (isLoading()) {
          <div class="skeleton-wrapper">
            @for (i of [1,2,3,4,5]; track i) {
              <div class="skeleton-row"></div>
            }
          </div>
        } @else if (requisitions().length === 0) {
          <div class="empty-state">
            <mat-icon>assignment</mat-icon>
            <h3>No requisitions found</h3>
            <p>{{ searchTerm ? 'Try a different search term.' : 'Create your first purchase requisition.' }}</p>
          </div>
        } @else {
          <table class="data-table">
            <thead>
              <tr>
                <th>Requisition</th>
                <th>Department</th>
                <th>Requested By</th>
                <th>Amount</th>
                <th>Status</th>
                <th>Date</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (req of requisitions(); track req.id) {
                <tr class="table-row">
                  <td>
                    <div class="req-number">{{ req.requisitionNumber }}</div>
                    <div class="secondary-text">{{ req.title }}</div>
                  </td>
                  <td class="secondary-text">{{ req.department }}</td>
                  <td class="secondary-text">{{ req.requestedByName }}</td>
                  <td>
                    <span class="amount">{{ req.currency }} {{ req.totalAmount | number:'1.0-0' }}</span>
                  </td>
                  <td>
                    <span class="status-badge" [class]="getStatusClass(req.status)">
                      {{ getStatusLabel(req.status) }}
                    </span>
                  </td>
                  <td class="secondary-text">{{ req.createdAt | date:'MMM d, y' }}</td>
                  <td>
                    <button mat-icon-button [matMenuTriggerFor]="menu" class="action-btn" (click)="$event.stopPropagation()">
                      <mat-icon>more_vert</mat-icon>
                    </button>
                    <mat-menu #menu="matMenu">
                      @if (req.status === 0) {
                        <button mat-menu-item (click)="submitRequisition(req.id)">
                          <mat-icon>send</mat-icon> Submit for Approval
                        </button>
                      }
                      @if (req.status === 1 || req.status === 2) {
                        <button mat-menu-item (click)="approveRequisition(req.id)">
                          <mat-icon>check_circle</mat-icon> Approve
                        </button>
                        <button mat-menu-item (click)="rejectRequisition(req.id)">
                          <mat-icon>cancel</mat-icon> Reject
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
                of {{ pg.totalCount }} requisitions
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

    <!-- Create Requisition Drawer -->
    @if (showCreateForm()) {
      <div class="drawer-overlay" (click)="closeCreateForm()"></div>
      <div class="drawer glass-card">
        <div class="drawer-header">
          <h2 class="drawer-title">New Purchase Requisition</h2>
          <button class="close-btn" (click)="closeCreateForm()">
            <mat-icon>close</mat-icon>
          </button>
        </div>

        @if (createError()) {
          <div class="error-banner">
            <mat-icon>error_outline</mat-icon>
            {{ createError() }}
          </div>
        }

        <form [formGroup]="createForm" (ngSubmit)="submitForm()" class="drawer-form">
          <div class="form-row">
            <div class="field-group">
              <label class="field-label">Title *</label>
              <input class="field-input" formControlName="title" placeholder="e.g. IT Equipment Q2" />
              @if (isFormFieldInvalid('title')) {
                <span class="field-error">Title is required.</span>
              }
            </div>
          </div>

          <div class="form-row two-col">
            <div class="field-group">
              <label class="field-label">Department *</label>
              <input class="field-input" formControlName="department" placeholder="e.g. Engineering" />
              @if (isFormFieldInvalid('department')) {
                <span class="field-error">Department is required.</span>
              }
            </div>
            <div class="field-group">
              <label class="field-label">Currency</label>
              <select class="field-select" formControlName="currency">
                <option value="USD">USD</option>
                <option value="EUR">EUR</option>
                <option value="GBP">GBP</option>
              </select>
            </div>
          </div>

          <div class="field-group">
            <label class="field-label">Description</label>
            <textarea class="field-textarea" formControlName="description" rows="2"
              placeholder="Brief description of the procurement need..."></textarea>
          </div>

          <!-- Line Items -->
          <div class="line-items-section">
            <div class="line-items-header">
              <h4>Line Items *</h4>
              <button type="button" class="btn-add-line" (click)="addLineItem()">
                <mat-icon>add</mat-icon> Add Item
              </button>
            </div>

            <div formArrayName="lineItems" class="line-items-list">
              @for (item of lineItemsArray.controls; track $index; let i = $index) {
                <div [formGroupName]="i" class="line-item-row glass-card">
                  <div class="line-item-header">
                    <span class="line-item-num">#{{ i + 1 }}</span>
                    <button type="button" class="remove-btn" (click)="removeLineItem(i)" *ngIf="lineItemsArray.length > 1">
                      <mat-icon>delete_outline</mat-icon>
                    </button>
                  </div>

                  <div class="form-row">
                    <div class="field-group" style="flex: 2">
                      <label class="field-label">Description *</label>
                      <input class="field-input" formControlName="itemDescription" placeholder="Item description" />
                    </div>
                    <div class="field-group">
                      <label class="field-label">Code</label>
                      <input class="field-input" formControlName="itemCode" placeholder="SKU-001" />
                    </div>
                    <div class="field-group">
                      <label class="field-label">Category</label>
                      <input class="field-input" formControlName="category" placeholder="IT" />
                    </div>
                  </div>

                  <div class="form-row three-col">
                    <div class="field-group">
                      <label class="field-label">Quantity *</label>
                      <input class="field-input" type="number" formControlName="quantity" min="1" />
                    </div>
                    <div class="field-group">
                      <label class="field-label">Unit</label>
                      <input class="field-input" formControlName="unitOfMeasure" placeholder="pcs" />
                    </div>
                    <div class="field-group">
                      <label class="field-label">Unit Price *</label>
                      <input class="field-input" type="number" formControlName="unitPrice" min="0.01" step="0.01" />
                    </div>
                  </div>

                  <div class="line-total">
                    Total: <strong>{{ createForm.value.currency }}
                    {{ ((item.value.quantity || 0) * (item.value.unitPrice || 0)) | number:'1.2-2' }}</strong>
                  </div>
                </div>
              }
            </div>

            <div class="grand-total">
              Grand Total:
              <strong>{{ createForm.value.currency }} {{ calculateTotal() | number:'1.2-2' }}</strong>
            </div>
          </div>

          <div class="drawer-actions">
            <button type="button" class="btn-secondary" (click)="closeCreateForm()">Cancel</button>
            <button type="submit" class="btn-primary" [disabled]="isSubmitting()">
              @if (isSubmitting()) {
                <span class="btn-spinner"></span> Creating...
              } @else {
                <mat-icon>save</mat-icon> Create Draft
              }
            </button>
          </div>
        </form>
      </div>
    }
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
      background: none; border: none; outline: none;
      color: $text-primary; font-size: $text-sm; flex: 1;
      &::placeholder { color: $text-muted; }
    }

    .clear-btn {
      background: none; border: none; cursor: pointer; color: $text-muted; padding: 0; display: flex;
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
    }

    .filter-chips { display: flex; gap: $space-xs; }

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

    .table-card { overflow: hidden; }

    .data-table { width: 100%; border-collapse: collapse; }

    thead tr { border-bottom: 1px solid $border-subtle; }

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

    td { padding: $space-md $space-lg; vertical-align: middle; }

    .req-number { font-size: $text-sm; font-weight: 600; color: $color-primary; }
    .secondary-text { font-size: $text-xs; color: $text-secondary; margin-top: 2px; }

    .amount { font-size: $text-sm; font-weight: 700; color: $text-primary; }

    .status-badge {
      padding: 4px $space-sm;
      border-radius: $radius-pill;
      font-size: $text-xs;
      font-weight: 600;
    }

    .status-draft     { background: rgba(255,255,255,0.06); color: $text-secondary; }
    .status-pending   { background: rgba($color-warning, 0.1); color: $color-warning; }
    .status-review    { background: rgba($color-info, 0.1);    color: $color-info; }
    .status-approved  { background: rgba($color-accent, 0.1);  color: $color-accent; }
    .status-danger    { background: rgba($color-danger, 0.1);  color: $color-danger; }
    .status-muted     { background: rgba(255,255,255,0.04);    color: $text-muted; }
    .status-converted { background: rgba($color-primary, 0.1); color: $color-primary; }

    .action-btn { color: $text-secondary; }

    .skeleton-wrapper { padding: $space-lg; }
    .skeleton-row {
      height: 56px;
      background: linear-gradient(90deg, rgba(255,255,255,0.04) 25%, rgba(255,255,255,0.08) 50%, rgba(255,255,255,0.04) 75%);
      background-size: 200% 100%;
      animation: shimmer 1.5s infinite;
      border-radius: $radius-md;
      margin-bottom: $space-sm;
    }
    @keyframes shimmer { 0% { background-position: -200% 0; } 100% { background-position: 200% 0; } }

    .empty-state {
      text-align: center; padding: $space-3xl; color: $text-secondary;
      mat-icon { font-size: 48px; width: 48px; height: 48px; color: $text-muted; margin-bottom: $space-md; }
      h3 { font-size: $text-xl; color: $text-primary; margin-bottom: $space-sm; }
      p { font-size: $text-sm; }
    }

    .pagination {
      display: flex; align-items: center; justify-content: space-between;
      padding: $space-md $space-lg; border-top: 1px solid $border-subtle;
    }
    .pagination-info { font-size: $text-sm; color: $text-secondary; }
    .pagination-controls { display: flex; align-items: center; gap: $space-sm; }
    .page-btn {
      width: 32px; height: 32px; border-radius: $radius-md; border: 1px solid $border-subtle;
      background: none; color: $text-secondary; cursor: pointer;
      display: flex; align-items: center; justify-content: center; transition: all $transition-fast;
      &:hover:not(:disabled) { border-color: $color-primary; color: $color-primary; }
      &:disabled { opacity: 0.3; cursor: not-allowed; }
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }
    .page-indicator { font-size: $text-sm; color: $text-secondary; padding: 0 $space-sm; }

    /* ─── Drawer ─── */
    .drawer-overlay {
      position: fixed; inset: 0; background: rgba(0,0,0,0.5); z-index: 200;
      backdrop-filter: blur(4px);
    }

    .drawer {
      position: fixed; right: 0; top: 0; bottom: 0; width: 620px;
      z-index: 201; overflow-y: auto;
      border-radius: $radius-lg 0 0 $radius-lg;
      padding: 0;
    }

    .drawer-header {
      display: flex; justify-content: space-between; align-items: center;
      padding: $space-xl $space-xl $space-lg;
      border-bottom: 1px solid $border-subtle;
      position: sticky; top: 0; background: inherit;
      backdrop-filter: blur(20px);
    }

    .drawer-title { font-family: $font-heading; font-size: $text-2xl; font-weight: 700; }

    .close-btn {
      background: none; border: none; cursor: pointer; color: $text-muted;
      padding: 4px; display: flex; align-items: center; border-radius: $radius-sm;
      transition: all $transition-fast;
      &:hover { background: rgba(255,255,255,0.06); color: $text-primary; }
      mat-icon { font-size: 22px; width: 22px; height: 22px; }
    }

    .error-banner {
      display: flex; align-items: center; gap: $space-sm;
      padding: $space-md $space-xl; margin: $space-md $space-xl 0;
      background: rgba($color-danger, 0.12); border: 1px solid rgba($color-danger, 0.3);
      border-radius: $radius-md; color: $color-danger; font-size: $text-sm;
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .drawer-form { padding: $space-xl; }

    .form-row { display: flex; gap: $space-lg; margin-bottom: 0; }
    .form-row.two-col > * { flex: 1; }
    .form-row.three-col > * { flex: 1; }

    .field-group {
      display: flex; flex-direction: column; gap: $space-xs; margin-bottom: $space-lg; flex: 1;
    }

    .field-label { font-size: $text-sm; font-weight: 500; color: $text-secondary; }

    .field-input, .field-select, .field-textarea {
      background: rgba(255,255,255,0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      color: $text-primary;
      font-size: $text-sm;
      padding: $space-sm $space-md;
      outline: none;
      font-family: $font-body;
      transition: border-color $transition-fast;

      &:focus { border-color: $color-primary; }
      &::placeholder { color: $text-muted; }
      option { background: $bg-secondary; }
    }

    .field-textarea { resize: vertical; min-height: 60px; }

    .field-error { font-size: $text-xs; color: $color-danger; }

    .line-items-section { margin-bottom: $space-xl; }

    .line-items-header {
      display: flex; justify-content: space-between; align-items: center;
      margin-bottom: $space-md;
      h4 { font-size: $text-base; font-weight: 600; }
    }

    .btn-add-line {
      display: flex; align-items: center; gap: 4px;
      padding: 6px $space-md; border-radius: $radius-md;
      border: 1px dashed $border-light; background: none;
      color: $color-primary; font-size: $text-sm; cursor: pointer;
      transition: all $transition-fast;
      &:hover { background: rgba($color-primary, 0.08); }
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .line-items-list { display: flex; flex-direction: column; gap: $space-md; }

    .line-item-row {
      padding: $space-md;
      border-radius: $radius-md;
    }

    .line-item-header {
      display: flex; justify-content: space-between; align-items: center;
      margin-bottom: $space-md;
    }

    .line-item-num { font-size: $text-xs; font-weight: 700; color: $color-primary; }

    .remove-btn {
      background: none; border: none; cursor: pointer; color: $color-danger;
      padding: 4px; display: flex; border-radius: $radius-sm;
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .line-total {
      text-align: right; font-size: $text-sm; color: $text-secondary;
      strong { color: $text-primary; }
    }

    .grand-total {
      text-align: right; font-size: $text-base; color: $text-secondary;
      padding: $space-md 0; border-top: 1px solid $border-subtle; margin-top: $space-md;
      strong { color: $color-accent; font-size: $text-xl; }
    }

    .drawer-actions {
      display: flex; justify-content: flex-end; gap: $space-md;
      padding-top: $space-lg; border-top: 1px solid $border-subtle;
    }

    .btn-secondary {
      padding: $space-sm $space-xl;
      border: 1px solid $border-subtle;
      background: none; color: $text-secondary;
      border-radius: $radius-md; font-size: $text-sm; cursor: pointer;
      transition: all $transition-fast;
      &:hover { border-color: $text-secondary; color: $text-primary; }
    }

    .btn-spinner {
      width: 16px; height: 16px;
      border: 2px solid rgba(255,255,255,0.3);
      border-top-color: white; border-radius: 50%;
      animation: spin 0.7s linear infinite; display: inline-block;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class RequisitionsComponent implements OnInit {
  requisitions = signal<RequisitionDto[]>([]);
  pagination = signal<PaginatedList<RequisitionDto> | null>(null);
  isLoading = signal(true);
  showCreateForm = signal(false);
  isSubmitting = signal(false);
  createError = signal('');

  searchTerm = '';
  selectedStatus: number | undefined = undefined;
  currentPage = 1;
  pageSize = 10;

  readonly Math = Math;

  createForm!: FormGroup;

  statusFilters = [
    { label: 'All', value: undefined as number | undefined },
    { label: 'Draft', value: 0 },
    { label: 'Submitted', value: 1 },
    { label: 'Under Review', value: 2 },
    { label: 'Approved', value: 3 },
    { label: 'Rejected', value: 4 },
  ];

  private searchDebounce: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private requisitionService: RequisitionService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.initForm();
    this.loadRequisitions();
  }

  private initForm(): void {
    this.createForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(300)]],
      description: [''],
      department: ['', Validators.required],
      currency: ['USD'],
      lineItems: this.fb.array([this.createLineItemGroup()])
    });
  }

  private createLineItemGroup(): FormGroup {
    return this.fb.group({
      itemDescription: ['', Validators.required],
      itemCode: [''],
      category: [''],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitOfMeasure: ['pcs'],
      unitPrice: [0, [Validators.required, Validators.min(0.01)]]
    });
  }

  get lineItemsArray(): FormArray {
    return this.createForm.get('lineItems') as FormArray;
  }

  addLineItem(): void {
    this.lineItemsArray.push(this.createLineItemGroup());
  }

  removeLineItem(index: number): void {
    this.lineItemsArray.removeAt(index);
  }

  calculateTotal(): number {
    return this.lineItemsArray.controls.reduce((sum, control) => {
      const qty = control.value.quantity || 0;
      const price = control.value.unitPrice || 0;
      return sum + qty * price;
    }, 0);
  }

  openCreateForm(): void {
    this.initForm();
    this.createError.set('');
    this.showCreateForm.set(true);
  }

  closeCreateForm(): void {
    this.showCreateForm.set(false);
  }

  submitForm(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.createError.set('');

    const request: CreateRequisitionRequest = {
      title: this.createForm.value.title,
      description: this.createForm.value.description,
      department: this.createForm.value.department,
      currency: this.createForm.value.currency,
      lineItems: this.createForm.value.lineItems
    };

    this.requisitionService.createRequisition(request).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeCreateForm();
        this.loadRequisitions();
      },
      error: (err) => {
        this.createError.set(err.error?.detail ?? 'Failed to create requisition.');
        this.isSubmitting.set(false);
      }
    });
  }

  loadRequisitions(): void {
    this.isLoading.set(true);
    this.requisitionService
      .getRequisitions(this.currentPage, this.pageSize, this.searchTerm || undefined, this.selectedStatus)
      .subscribe({
        next: (data) => {
          this.requisitions.set(data.items);
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
      this.loadRequisitions();
    }, 350);
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.currentPage = 1;
    this.loadRequisitions();
  }

  setStatusFilter(status: number | undefined): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadRequisitions();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadRequisitions();
  }

  submitRequisition(id: string): void {
    this.requisitionService.submitRequisition(id).subscribe(() => this.loadRequisitions());
  }

  approveRequisition(id: string): void {
    this.requisitionService.approveRequisition(id).subscribe(() => this.loadRequisitions());
  }

  rejectRequisition(id: string): void {
    const reason = prompt('Enter rejection reason:');
    if (reason) {
      this.requisitionService.rejectRequisition(id, reason).subscribe(() => this.loadRequisitions());
    }
  }

  isFormFieldInvalid(field: string): boolean {
    const control = this.createForm.get(field);
    return !!(control && control.invalid && control.touched);
  }

  getStatusLabel(status: number): string {
    return REQUISITION_STATUS_LABELS[status] ?? 'Unknown';
  }

  getStatusClass(status: number): string {
    return REQUISITION_STATUS_CLASSES[status] ?? '';
  }
}
