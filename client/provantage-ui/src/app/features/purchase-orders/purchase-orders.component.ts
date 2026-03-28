import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import {
  PurchaseOrderService, PurchaseOrderDto,
  PO_STATUS_LABELS, PO_STATUS_CLASSES,
  CreatePurchaseOrderRequest
} from '../../core/services/purchase-order.service';
import { VendorService } from '../../core/services/vendor.service';

@Component({
  selector: 'app-purchase-orders',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
<div class="page-container">
  <!-- Header -->
  <div class="page-header">
    <div class="header-left">
      <h1 class="page-title">Purchase Orders</h1>
      <p class="page-subtitle">Manage and track purchase orders sent to vendors</p>
    </div>
    <button class="btn-primary" (click)="openCreateDrawer()">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
        <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
      </svg>
      New Purchase Order
    </button>
  </div>

  <!-- Filters -->
  <div class="filters-bar">
    <div class="search-box">
      <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
      </svg>
      <input
        type="text"
        placeholder="Search by order number or vendor..."
        class="search-input"
        [value]="searchTerm()"
        (input)="onSearch($event)"/>
    </div>
    <div class="filter-chips">
      @for (chip of statusChips; track chip.value) {
        <button
          class="chip"
          [class.chip-active]="selectedStatus() === chip.value"
          (click)="setStatus(chip.value)">
          {{chip.label}}
        </button>
      }
    </div>
  </div>

  <!-- Table -->
  <div class="table-card">
    @if (loading()) {
      <div class="skeleton-rows">
        @for (i of [1,2,3,4,5]; track i) {
          <div class="skeleton-row">
            <div class="skeleton-cell wide shimmer"></div>
            <div class="skeleton-cell shimmer"></div>
            <div class="skeleton-cell shimmer"></div>
            <div class="skeleton-cell shimmer"></div>
            <div class="skeleton-cell narrow shimmer"></div>
          </div>
        }
      </div>
    } @else if (orders().length === 0) {
      <div class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.3">
          <path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2"/>
          <rect x="9" y="3" width="6" height="4" rx="2"/>
        </svg>
        <p>No purchase orders found</p>
      </div>
    } @else {
      <table class="data-table">
        <thead>
          <tr>
            <th>Order #</th>
            <th>Vendor</th>
            <th>Amount</th>
            <th>Expected Delivery</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @for (order of orders(); track order.id) {
            <tr>
              <td>
                <div class="order-number">{{order.orderNumber}}</div>
                @if (order.requisitionNumber) {
                  <div class="sub-text">From {{order.requisitionNumber}}</div>
                }
              </td>
              <td>
                <div class="vendor-name">{{order.vendorName}}</div>
                <div class="sub-text">{{order.paymentTerms}}</div>
              </td>
              <td>
                <span class="amount">{{order.currency}} {{order.totalAmount | number:'1.2-2'}}</span>
              </td>
              <td>
                <span [class.overdue]="isOverdue(order.expectedDeliveryDate, order.status)">
                  {{order.expectedDeliveryDate | date:'mediumDate'}}
                </span>
              </td>
              <td>
                <span class="status-badge" [ngClass]="getStatusClass(order.status)">
                  {{getStatusLabel(order.status)}}
                </span>
              </td>
              <td>
                <div class="action-menu-wrapper">
                  <button class="action-btn" (click)="toggleMenu(order.id)">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                      <circle cx="12" cy="5" r="1.5"/><circle cx="12" cy="12" r="1.5"/><circle cx="12" cy="19" r="1.5"/>
                    </svg>
                  </button>
                  @if (activeMenu() === order.id) {
                    <div class="action-dropdown">
                      @if (order.status === 0) {
                        <button (click)="changeStatus(order.id, 1); closeMenu()">Mark as Sent</button>
                      }
                      @if (order.status === 1) {
                        <button (click)="changeStatus(order.id, 2); closeMenu()">Mark Acknowledged</button>
                      }
                      @if (order.status === 2 || order.status === 3) {
                        <button (click)="changeStatus(order.id, 4); closeMenu()">Mark Received</button>
                      }
                      @if (order.status === 4) {
                        <button (click)="changeStatus(order.id, 5); closeMenu()">Close Order</button>
                      }
                      @if (order.status === 0 || order.status === 1) {
                        <button class="danger" (click)="changeStatus(order.id, 6); closeMenu()">Cancel Order</button>
                      }
                    </div>
                  }
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>

      <!-- Pagination -->
      <div class="pagination">
        <span class="pagination-info">
          Showing {{paginationStart()}}–{{paginationEnd()}} of {{totalCount()}}
        </span>
        <div class="pagination-controls">
          <button class="page-btn" [disabled]="currentPage() === 1" (click)="prevPage()">‹ Prev</button>
          <button class="page-btn" [disabled]="!hasNextPage()" (click)="nextPage()">Next ›</button>
        </div>
      </div>
    }
  </div>
</div>

<!-- Create Drawer Overlay -->
@if (drawerOpen()) {
  <div class="drawer-overlay" (click)="closeDrawer()"></div>
  <div class="drawer" [class.drawer-open]="drawerOpen()">
    <div class="drawer-header">
      <h2>New Purchase Order</h2>
      <button class="close-btn" (click)="closeDrawer()">✕</button>
    </div>

    <form [formGroup]="poForm" (ngSubmit)="submitPo()" class="drawer-body">
      @if (formError()) {
        <div class="error-banner">{{formError()}}</div>
      }

      <div class="form-group">
        <label class="form-label">Vendor <span class="required">*</span></label>
        <select class="form-control" formControlName="vendorId">
          <option value="">Select vendor...</option>
          @for (v of availableVendors(); track v.id) {
            <option [value]="v.id">{{v.companyName}}</option>
          }
        </select>
      </div>

      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Expected Delivery <span class="required">*</span></label>
          <input type="date" class="form-control" formControlName="expectedDeliveryDate"/>
        </div>
        <div class="form-group">
          <label class="form-label">Payment Terms</label>
          <select class="form-control" formControlName="paymentTerms">
            <option>Net 30</option>
            <option>Net 60</option>
            <option>Net 90</option>
            <option>Immediate</option>
          </select>
        </div>
      </div>

      <div class="form-group">
        <label class="form-label">Shipping Address <span class="required">*</span></label>
        <input type="text" class="form-control" formControlName="shippingAddress" placeholder="123 Main St, City, Country"/>
      </div>

      <div class="form-group">
        <label class="form-label">Notes</label>
        <textarea class="form-control" formControlName="notes" rows="2" placeholder="Optional notes..."></textarea>
      </div>

      <!-- Line Items -->
      <div class="section-heading">
        <span>Line Items</span>
        <button type="button" class="btn-add-item" (click)="addLineItem()">+ Add Item</button>
      </div>

      <div formArrayName="lineItems" class="line-items">
        @for (item of lineItemControls; track $index) {
          <div [formGroupName]="$index" class="line-item-row">
            <div class="li-description">
              <input type="text" class="form-control" formControlName="itemDescription" placeholder="Description"/>
            </div>
            <div class="li-code">
              <input type="text" class="form-control" formControlName="itemCode" placeholder="Code"/>
            </div>
            <div class="li-qty">
              <input type="number" class="form-control" formControlName="quantityOrdered" placeholder="Qty" min="1"/>
            </div>
            <div class="li-price">
              <input type="number" class="form-control" formControlName="unitPrice" placeholder="Unit Price" min="0.01" step="0.01"/>
            </div>
            <div class="li-currency">
              <select class="form-control" formControlName="currency">
                <option>USD</option><option>EUR</option><option>GBP</option><option>INR</option>
              </select>
            </div>
            <button type="button" class="btn-remove-item" (click)="removeLineItem($index)"
              [disabled]="lineItemControls.length === 1">✕</button>
          </div>
        }
      </div>

      <div class="total-summary">
        <span>Total</span>
        <span class="total-amount">{{poTotal | number:'1.2-2'}}</span>
      </div>

      <div class="drawer-footer">
        <button type="button" class="btn-secondary" (click)="closeDrawer()">Cancel</button>
        <button type="submit" class="btn-primary" [disabled]="poForm.invalid || submitting()">
          @if (submitting()) {<span class="spinner"></span>} Create PO
        </button>
      </div>
    </form>
  </div>
}
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 1400px; }
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; }
    .page-title { font-size: 24px; font-weight: 700; color: var(--text-primary); margin: 0 0 4px; }
    .page-subtitle { font-size: 14px; color: var(--text-secondary); margin: 0; }

    .filters-bar { display: flex; gap: 16px; align-items: center; margin-bottom: 20px; flex-wrap: wrap; }
    .search-box { position: relative; flex: 1; min-width: 260px; }
    .search-icon { position: absolute; left: 12px; top: 50%; transform: translateY(-50%); color: var(--text-secondary); }
    .search-input { width: 100%; padding: 10px 12px 10px 36px; background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 14px; box-sizing: border-box; }
    .search-input:focus { outline: none; border-color: var(--primary); }

    .filter-chips { display: flex; gap: 8px; flex-wrap: wrap; }
    .chip { padding: 6px 14px; border-radius: 20px; border: 1px solid var(--border); background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; transition: all 0.2s; }
    .chip:hover { border-color: var(--primary); color: var(--primary); }
    .chip-active { background: var(--primary); border-color: var(--primary); color: #fff !important; }

    .table-card { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; overflow: hidden; }
    .data-table { width: 100%; border-collapse: collapse; }
    .data-table th { padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 600; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid var(--border); background: var(--surface-2); }
    .data-table td { padding: 14px 16px; border-bottom: 1px solid var(--border); font-size: 14px; color: var(--text-primary); vertical-align: middle; }
    .data-table tbody tr:last-child td { border-bottom: none; }
    .data-table tbody tr:hover { background: var(--surface-2); }

    .order-number { font-weight: 600; color: var(--primary); font-family: 'SF Mono', monospace; font-size: 13px; }
    .vendor-name { font-weight: 500; }
    .sub-text { font-size: 12px; color: var(--text-secondary); margin-top: 2px; }
    .amount { font-weight: 600; font-size: 14px; }
    .overdue { color: var(--danger) !important; font-weight: 500; }

    .status-badge { padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 500; }
    :host ::ng-deep .status-draft { background: rgba(148,163,184,.12); color: #94a3b8; }
    :host ::ng-deep .status-pending { background: rgba(251,191,36,.12); color: #f59e0b; }
    :host ::ng-deep .status-review { background: rgba(99,102,241,.12); color: #818cf8; }
    :host ::ng-deep .status-warning { background: rgba(245,158,11,.12); color: #f59e0b; }
    :host ::ng-deep .status-approved { background: rgba(52,211,153,.12); color: #34d399; }
    :host ::ng-deep .status-muted { background: rgba(100,116,139,.12); color: #64748b; }
    :host ::ng-deep .status-danger { background: rgba(239,68,68,.12); color: #ef4444; }

    .action-menu-wrapper { position: relative; }
    .action-btn { background: none; border: none; color: var(--text-secondary); cursor: pointer; padding: 4px 8px; border-radius: 4px; }
    .action-btn:hover { background: var(--surface-2); color: var(--text-primary); }
    .action-dropdown { position: absolute; right: 0; top: calc(100% + 4px); background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; min-width: 160px; z-index: 100; box-shadow: 0 8px 24px rgba(0,0,0,.3); }
    .action-dropdown button { display: block; width: 100%; padding: 9px 14px; text-align: left; background: none; border: none; color: var(--text-primary); font-size: 13px; cursor: pointer; }
    .action-dropdown button:hover { background: var(--surface); color: var(--primary); }
    .action-dropdown button.danger { color: var(--danger); }

    .pagination { display: flex; justify-content: space-between; align-items: center; padding: 14px 16px; border-top: 1px solid var(--border); }
    .pagination-info { font-size: 13px; color: var(--text-secondary); }
    .pagination-controls { display: flex; gap: 8px; }
    .page-btn { padding: 6px 14px; border: 1px solid var(--border); border-radius: 6px; background: var(--surface-2); color: var(--text-primary); font-size: 13px; cursor: pointer; }
    .page-btn:hover:not(:disabled) { border-color: var(--primary); color: var(--primary); }
    .page-btn:disabled { opacity: 0.4; cursor: not-allowed; }

    .empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 60px; gap: 12px; color: var(--text-secondary); }

    .skeleton-rows { padding: 12px 0; }
    .skeleton-row { display: flex; gap: 16px; padding: 14px 16px; align-items: center; border-bottom: 1px solid var(--border); }
    .skeleton-cell { height: 14px; border-radius: 4px; flex: 1; }
    .skeleton-cell.wide { flex: 2; }
    .skeleton-cell.narrow { flex: 0.5; }
    @keyframes shimmer { 0%{background-position:-200px 0} 100%{background-position:calc(200px + 100%) 0} }
    .shimmer { background: linear-gradient(90deg, var(--surface-2) 25%, var(--surface) 50%, var(--surface-2) 75%); background-size: 200px 100%; animation: shimmer 1.2s infinite; }

    /* Drawer */
    .drawer-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.6); z-index: 200; }
    .drawer { position: fixed; top: 0; right: 0; width: 640px; height: 100vh; background: var(--surface); border-left: 1px solid var(--border); z-index: 201; display: flex; flex-direction: column; transform: translateX(100%); transition: transform 0.3s ease; overflow: hidden; }
    .drawer.drawer-open { transform: translateX(0); }
    .drawer-header { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--border); }
    .drawer-header h2 { font-size: 18px; font-weight: 700; margin: 0; }
    .close-btn { background: none; border: none; font-size: 18px; color: var(--text-secondary); cursor: pointer; padding: 4px 8px; border-radius: 4px; }
    .close-btn:hover { background: var(--surface-2); color: var(--text-primary); }
    .drawer-body { flex: 1; overflow-y: auto; padding: 24px; display: flex; flex-direction: column; gap: 16px; }
    .drawer-footer { padding: 16px 24px; border-top: 1px solid var(--border); display: flex; justify-content: flex-end; gap: 12px; background: var(--surface); }

    .form-group { display: flex; flex-direction: column; gap: 6px; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .form-label { font-size: 13px; font-weight: 500; color: var(--text-secondary); }
    .required { color: var(--danger); }
    .form-control { padding: 9px 12px; background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 14px; }
    .form-control:focus { outline: none; border-color: var(--primary); }
    textarea.form-control { resize: vertical; }

    .section-heading { display: flex; justify-content: space-between; align-items: center; font-size: 13px; font-weight: 600; color: var(--text-secondary); padding: 4px 0; border-bottom: 1px solid var(--border); }
    .btn-add-item { background: none; border: none; color: var(--primary); font-size: 13px; cursor: pointer; font-weight: 500; }

    .line-items { display: flex; flex-direction: column; gap: 8px; }
    .line-item-row { display: grid; grid-template-columns: 2fr 1fr 0.7fr 0.9fr 0.7fr 28px; gap: 6px; align-items: center; }
    .btn-remove-item { background: none; border: none; color: var(--text-secondary); cursor: pointer; font-size: 14px; padding: 4px; border-radius: 4px; }
    .btn-remove-item:hover:not(:disabled) { color: var(--danger); background: rgba(239,68,68,.1); }
    .btn-remove-item:disabled { opacity: 0.3; cursor: not-allowed; }

    .total-summary { display: flex; justify-content: space-between; padding: 12px 0; border-top: 1px solid var(--border); font-size: 14px; font-weight: 600; color: var(--text-primary); }
    .total-amount { color: var(--primary); font-size: 16px; }

    .error-banner { padding: 10px 14px; background: rgba(239,68,68,.1); border: 1px solid rgba(239,68,68,.3); border-radius: 8px; color: var(--danger); font-size: 13px; }

    .btn-primary { display: flex; align-items: center; gap: 8px; padding: 9px 18px; background: var(--primary); border: none; border-radius: 8px; color: #fff; font-size: 14px; font-weight: 500; cursor: pointer; }
    .btn-primary:hover:not(:disabled) { opacity: 0.9; }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-secondary { padding: 9px 18px; background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 14px; cursor: pointer; }
    .btn-secondary:hover { border-color: var(--primary); color: var(--primary); }

    .spinner { width: 14px; height: 14px; border: 2px solid rgba(255,255,255,.3); border-top-color: #fff; border-radius: 50%; animation: spin .6s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }
  `]
})
export class PurchaseOrdersComponent implements OnInit {
  private poService = inject(PurchaseOrderService);
  private vendorService = inject(VendorService);
  private fb = inject(FormBuilder);

  orders = signal<PurchaseOrderDto[]>([]);
  loading = signal(true);
  totalCount = signal(0);
  currentPage = signal(1);
  hasNextPage = signal(false);
  searchTerm = signal('');
  selectedStatus = signal<number | null>(null);
  activeMenu = signal<string | null>(null);
  drawerOpen = signal(false);
  submitting = signal(false);
  formError = signal<string | null>(null);
  availableVendors = signal<{id: string; companyName: string}[]>([]);

  private searchSubject = new Subject<string>();
  readonly pageSize = 10;
  Math = Math;

  statusChips = [
    { label: 'All', value: null },
    { label: 'Created', value: 0 },
    { label: 'Sent', value: 1 },
    { label: 'Received', value: 4 },
    { label: 'Closed', value: 5 },
    { label: 'Cancelled', value: 6 },
  ];

  poForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadOrders();
    this.loadVendors();

    this.searchSubject.pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(term => {
        this.searchTerm.set(term);
        this.currentPage.set(1);
        this.loadOrders();
      });
  }

  initForm(): void {
    this.poForm = this.fb.group({
      vendorId: ['', Validators.required],
      expectedDeliveryDate: ['', Validators.required],
      paymentTerms: ['Net 30'],
      shippingAddress: ['', Validators.required],
      notes: [''],
      lineItems: this.fb.array([this.createLineItem()])
    });
  }

  createLineItem() {
    return this.fb.group({
      itemDescription: ['', Validators.required],
      itemCode: [''],
      unitOfMeasure: ['EA'],
      quantityOrdered: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0.01)]],
      currency: ['USD']
    });
  }

  get lineItemControls() {
    return (this.poForm.get('lineItems') as FormArray).controls as FormGroup[];
  }

  get poTotal(): number {
    return this.lineItemControls.reduce((sum, ctrl) => {
      const qty = Number(ctrl.get('quantityOrdered')?.value ?? 0);
      const price = Number(ctrl.get('unitPrice')?.value ?? 0);
      return sum + qty * price;
    }, 0);
  }

  addLineItem(): void {
    (this.poForm.get('lineItems') as FormArray).push(this.createLineItem());
  }

  removeLineItem(index: number): void {
    (this.poForm.get('lineItems') as FormArray).removeAt(index);
  }

  loadOrders(): void {
    this.loading.set(true);
    this.poService.getPurchaseOrders(
      this.currentPage(),
      this.pageSize,
      this.searchTerm() || undefined,
      this.selectedStatus() ?? undefined
    ).subscribe({
      next: (result) => {
        this.orders.set(result.items);
        this.totalCount.set(result.totalCount);
        this.hasNextPage.set(result.hasNextPage);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  loadVendors(): void {
    this.vendorService.getVendors(1, 100, undefined, 1) // status=1 (Approved)
      .subscribe(result => this.availableVendors.set(result.items));
  }

  onSearch(event: Event): void {
    this.searchSubject.next((event.target as HTMLInputElement).value);
  }

  setStatus(status: number | null): void {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
    this.loadOrders();
  }

  toggleMenu(id: string): void {
    this.activeMenu.set(this.activeMenu() === id ? null : id);
  }

  closeMenu(): void {
    this.activeMenu.set(null);
  }

  changeStatus(id: string, status: number): void {
    this.poService.updateStatus(id, status).subscribe({
      next: () => this.loadOrders()
    });
  }

  openCreateDrawer(): void {
    this.initForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }

  submitPo(): void {
    if (this.poForm.invalid) return;
    this.submitting.set(true);
    this.formError.set(null);

    const v = this.poForm.value;
    const request: CreatePurchaseOrderRequest = {
      vendorId: v.vendorId,
      expectedDeliveryDate: v.expectedDeliveryDate,
      paymentTerms: v.paymentTerms,
      shippingAddress: v.shippingAddress,
      notes: v.notes || undefined,
      lineItems: v.lineItems
    };

    this.poService.createPurchaseOrder(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeDrawer();
        this.loadOrders();
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(err.error?.detail ?? err.error?.error ?? 'Failed to create purchase order.');
      }
    });
  }

  getStatusLabel(status: number): string { return PO_STATUS_LABELS[status] ?? 'Unknown'; }
  getStatusClass(status: number): string { return PO_STATUS_CLASSES[status] ?? ''; }

  isOverdue(date: string, status: number): boolean {
    if (status === 4 || status === 5 || status === 6) return false;
    return new Date(date) < new Date();
  }

  paginationStart(): number { return (this.currentPage() - 1) * this.pageSize + 1; }
  paginationEnd(): number { return Math.min(this.currentPage() * this.pageSize, this.totalCount()); }
  prevPage(): void { if (this.currentPage() > 1) { this.currentPage.update(p => p - 1); this.loadOrders(); } }
  nextPage(): void { if (this.hasNextPage()) { this.currentPage.update(p => p + 1); this.loadOrders(); } }
}
