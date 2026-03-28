import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import {
  InvoiceService, InvoiceDto, InvoiceDetailDto, ThreeWayMatchResultDto,
  INVOICE_STATUS_LABELS, INVOICE_STATUS_CLASSES,
  CreateInvoiceRequest
} from '../../core/services/invoice.service';

@Component({
  selector: 'app-invoices',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
<div class="page-container">
  <!-- Header -->
  <div class="page-header">
    <div class="header-left">
      <h1 class="page-title">Invoices</h1>
      <p class="page-subtitle">Record vendor invoices and run three-way matching</p>
    </div>
    <button class="btn-primary" (click)="openCreateDrawer()">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
        <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
      </svg>
      Record Invoice
    </button>
  </div>

  <!-- Filters -->
  <div class="filters-bar">
    <div class="search-box">
      <svg class="search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
        <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
      </svg>
      <input type="text" placeholder="Search invoice number, vendor..."
        class="search-input" [value]="searchTerm()" (input)="onSearch($event)"/>
    </div>
    <div class="filter-chips">
      @for (chip of statusChips; track chip.value) {
        <button class="chip" [class.chip-active]="selectedStatus() === chip.value"
          (click)="setStatus(chip.value)">{{chip.label}}</button>
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
            <div class="skeleton-cell narrow shimmer"></div>
          </div>
        }
      </div>
    } @else if (invoices().length === 0) {
      <div class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.3">
          <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
          <polyline points="14 2 14 8 20 8"/>
          <line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/>
          <polyline points="10 9 9 9 8 9"/>
        </svg>
        <p>No invoices found</p>
      </div>
    } @else {
      <table class="data-table">
        <thead>
          <tr>
            <th>Invoice #</th>
            <th>Vendor / PO</th>
            <th>Amount</th>
            <th>Due Date</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          @for (inv of invoices(); track inv.id) {
            <tr>
              <td>
                <div class="inv-ref">{{inv.internalReference}}</div>
                <div class="sub-text">Vendor: {{inv.invoiceNumber}}</div>
              </td>
              <td>
                <div class="vendor-name">{{inv.vendorName}}</div>
                <div class="sub-text">{{inv.purchaseOrderNumber}}</div>
              </td>
              <td><span class="amount">{{inv.currency}} {{inv.totalAmount | number:'1.2-2'}}</span></td>
              <td>
                <span [class.overdue]="isOverdue(inv.dueDate, inv.status)">
                  {{inv.dueDate | date:'mediumDate'}}
                </span>
              </td>
              <td>
                <span class="status-badge" [ngClass]="getStatusClass(inv.status)">
                  {{getStatusLabel(inv.status)}}
                </span>
              </td>
              <td>
                <div class="action-menu-wrapper">
                  <button class="action-btn" (click)="toggleMenu(inv.id)">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                      <circle cx="12" cy="5" r="1.5"/><circle cx="12" cy="12" r="1.5"/><circle cx="12" cy="19" r="1.5"/>
                    </svg>
                  </button>
                  @if (activeMenu() === inv.id) {
                    <div class="action-dropdown">
                      @if (inv.status === 0 || inv.status === 3) {
                        <button (click)="runMatch(inv.id); closeMenu()">
                          ⚡ Run 3-Way Match
                        </button>
                      }
                      @if (inv.status === 1) {
                        <button (click)="viewMatchResult(inv.id); closeMenu()">
                          View Match Result
                        </button>
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
        <span class="pagination-info">Showing {{paginationStart()}}–{{paginationEnd()}} of {{totalCount()}}</span>
        <div class="pagination-controls">
          <button class="page-btn" [disabled]="currentPage() === 1" (click)="prevPage()">‹ Prev</button>
          <button class="page-btn" [disabled]="!hasNextPage()" (click)="nextPage()">Next ›</button>
        </div>
      </div>
    }
  </div>
</div>

<!-- Create Invoice Drawer -->
@if (drawerOpen()) {
  <div class="drawer-overlay" (click)="closeDrawer()"></div>
  <div class="drawer drawer-open">
    <div class="drawer-header">
      <h2>Record Invoice</h2>
      <button class="close-btn" (click)="closeDrawer()">✕</button>
    </div>
    <form [formGroup]="invForm" (ngSubmit)="submitInvoice()" class="drawer-body">
      @if (formError()) {
        <div class="error-banner">{{formError()}}</div>
      }
      <div class="form-group">
        <label class="form-label">Vendor Invoice Number <span class="required">*</span></label>
        <input type="text" class="form-control" formControlName="invoiceNumber" placeholder="e.g. INV-2026-001"/>
      </div>
      <div class="form-group">
        <label class="form-label">Purchase Order ID <span class="required">*</span></label>
        <input type="text" class="form-control" formControlName="purchaseOrderId" placeholder="PO UUID"/>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Invoice Date <span class="required">*</span></label>
          <input type="date" class="form-control" formControlName="invoiceDate"/>
        </div>
        <div class="form-group">
          <label class="form-label">Due Date <span class="required">*</span></label>
          <input type="date" class="form-control" formControlName="dueDate"/>
        </div>
      </div>
      <!-- Line Items -->
      <div class="section-heading">
        <span>Invoice Lines</span>
        <button type="button" class="btn-add-item" (click)="addInvLine()">+ Add Line</button>
      </div>
      <div formArrayName="lineItems" class="line-items">
        @for (item of invLineControls; track $index) {
          <div [formGroupName]="$index" class="line-item-row">
            <div class="li-description">
              <input type="text" class="form-control" formControlName="itemDescription" placeholder="Description"/>
            </div>
            <div class="li-code">
              <input type="text" class="form-control" formControlName="itemCode" placeholder="Item Code" />
            </div>
            <div class="li-qty">
              <input type="number" class="form-control" formControlName="quantity" placeholder="Qty"/>
            </div>
            <div class="li-price">
              <input type="number" class="form-control" formControlName="unitPrice" placeholder="Price"/>
            </div>
            <div class="li-currency">
              <select class="form-control" formControlName="currency">
                <option>USD</option><option>EUR</option><option>GBP</option><option>INR</option>
              </select>
            </div>
            <button type="button" class="btn-remove-item" (click)="removeInvLine($index)"
              [disabled]="invLineControls.length === 1">✕</button>
          </div>
        }
      </div>
      <div class="drawer-footer">
        <button type="button" class="btn-secondary" (click)="closeDrawer()">Cancel</button>
        <button type="submit" class="btn-primary" [disabled]="invForm.invalid || submitting()">
          @if (submitting()) {<span class="spinner"></span>} Record Invoice
        </button>
      </div>
    </form>
  </div>
}

<!-- Three-Way Match Result Modal -->
@if (matchResult()) {
  <div class="modal-overlay" (click)="closeMatchModal()">
    <div class="modal-card" (click)="$event.stopPropagation()">
      <div class="modal-header">
        <div class="modal-title-area">
          <h2>Three-Way Match Result</h2>
          <span class="match-badge" [class.matched]="matchResult()!.isFullyMatched"
            [class.disputed]="!matchResult()!.isFullyMatched">
            {{matchResult()!.isFullyMatched ? '✓ Matched' : '⚠ Disputed'}}
          </span>
        </div>
        <button class="close-btn" (click)="closeMatchModal()">✕</button>
      </div>
      <div class="match-summary">{{matchResult()!.summary}}</div>

      <div class="match-table-wrapper">
        <table class="match-table">
          <thead>
            <tr>
              <th>Item Code</th>
              <th>Description</th>
              <th class="num-col">Inv Qty</th>
              <th class="num-col">Inv Price</th>
              <th class="num-col">PO Price</th>
              <th class="num-col">Received</th>
              <th class="num-col">Price Var%</th>
              <th class="center-col">Match</th>
            </tr>
          </thead>
          <tbody>
            @for (line of matchResult()!.lineResults; track line.itemCode) {
              <tr [class.row-matched]="line.isMatched" [class.row-disputed]="!line.isMatched">
                <td class="mono">{{line.itemCode}}</td>
                <td>{{line.itemDescription}}</td>
                <td class="num-col">{{line.invoiceQty}}</td>
                <td class="num-col" [class.variance-warn]="!line.isPriceMatched">
                  {{line.invoiceUnitPrice | number:'1.2-2'}}
                </td>
                <td class="num-col">{{line.poUnitPrice | number:'1.2-2'}}</td>
                <td class="num-col" [class.variance-warn]="!line.isQuantityMatched">
                  {{line.receivedQty}}
                </td>
                <td class="num-col" [class.variance-warn]="!line.isPriceMatched">
                  {{line.priceVariancePercent | number:'1.1-1'}}%
                </td>
                <td class="center-col">
                  @if (line.isMatched) {
                    <span class="match-icon matched">✓</span>
                  } @else {
                    <span class="match-icon disputed" [title]="line.discrepancyNote ?? ''">✗</span>
                  }
                </td>
              </tr>
              @if (line.discrepancyNote && !line.isMatched) {
                <tr class="discrepancy-row">
                  <td colspan="8"><span class="discrepancy-note">⚠ {{line.discrepancyNote}}</span></td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>
      <div class="modal-footer">
        <button class="btn-secondary" (click)="closeMatchModal()">Close</button>
      </div>
    </div>
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

    .inv-ref { font-weight: 600; color: var(--primary); font-size: 13px; font-family: 'SF Mono', monospace; }
    .vendor-name { font-weight: 500; }
    .sub-text { font-size: 12px; color: var(--text-secondary); margin-top: 2px; }
    .amount { font-weight: 600; }
    .overdue { color: var(--danger) !important; font-weight: 500; }

    .status-badge { padding: 3px 10px; border-radius: 20px; font-size: 12px; font-weight: 500; }
    :host ::ng-deep .status-pending { background: rgba(251,191,36,.12); color: #f59e0b; }
    :host ::ng-deep .status-approved { background: rgba(52,211,153,.12); color: #34d399; }
    :host ::ng-deep .status-review { background: rgba(99,102,241,.12); color: #818cf8; }
    :host ::ng-deep .status-danger { background: rgba(239,68,68,.12); color: #ef4444; }
    :host ::ng-deep .status-muted { background: rgba(100,116,139,.12); color: #64748b; }
    :host ::ng-deep .status-converted { background: rgba(16,185,129,.12); color: #10b981; }

    .action-menu-wrapper { position: relative; }
    .action-btn { background: none; border: none; color: var(--text-secondary); cursor: pointer; padding: 4px 8px; border-radius: 4px; }
    .action-btn:hover { background: var(--surface-2); }
    .action-dropdown { position: absolute; right: 0; top: calc(100% + 4px); background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; min-width: 170px; z-index: 100; box-shadow: 0 8px 24px rgba(0,0,0,.3); }
    .action-dropdown button { display: block; width: 100%; padding: 9px 14px; text-align: left; background: none; border: none; color: var(--text-primary); font-size: 13px; cursor: pointer; }
    .action-dropdown button:hover { background: var(--surface); color: var(--primary); }

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
    .drawer { position: fixed; top: 0; right: 0; width: 640px; height: 100vh; background: var(--surface); border-left: 1px solid var(--border); z-index: 201; display: flex; flex-direction: column; transform: translateX(0); overflow: hidden; }
    .drawer-header { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--border); }
    .drawer-header h2 { font-size: 18px; font-weight: 700; margin: 0; }
    .close-btn { background: none; border: none; font-size: 18px; color: var(--text-secondary); cursor: pointer; padding: 4px 8px; border-radius: 4px; }
    .close-btn:hover { background: var(--surface-2); }
    .drawer-body { flex: 1; overflow-y: auto; padding: 24px; display: flex; flex-direction: column; gap: 16px; }
    .drawer-footer { padding: 16px 24px; border-top: 1px solid var(--border); display: flex; justify-content: flex-end; gap: 12px; background: var(--surface); }

    .form-group { display: flex; flex-direction: column; gap: 6px; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .form-label { font-size: 13px; font-weight: 500; color: var(--text-secondary); }
    .required { color: var(--danger); }
    .form-control { padding: 9px 12px; background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 14px; }
    .form-control:focus { outline: none; border-color: var(--primary); }

    .section-heading { display: flex; justify-content: space-between; align-items: center; font-size: 13px; font-weight: 600; color: var(--text-secondary); padding: 4px 0; border-bottom: 1px solid var(--border); }
    .btn-add-item { background: none; border: none; color: var(--primary); font-size: 13px; cursor: pointer; font-weight: 500; }

    .line-items { display: flex; flex-direction: column; gap: 8px; }
    .line-item-row { display: grid; grid-template-columns: 2fr 1fr 0.7fr 0.9fr 0.7fr 28px; gap: 6px; align-items: center; }
    .btn-remove-item { background: none; border: none; color: var(--text-secondary); cursor: pointer; font-size: 14px; padding: 4px; border-radius: 4px; }
    .btn-remove-item:hover:not(:disabled) { color: var(--danger); background: rgba(239,68,68,.1); }
    .btn-remove-item:disabled { opacity: 0.3; cursor: not-allowed; }

    .error-banner { padding: 10px 14px; background: rgba(239,68,68,.1); border: 1px solid rgba(239,68,68,.3); border-radius: 8px; color: var(--danger); font-size: 13px; }

    .btn-primary { display: flex; align-items: center; gap: 8px; padding: 9px 18px; background: var(--primary); border: none; border-radius: 8px; color: #fff; font-size: 14px; font-weight: 500; cursor: pointer; }
    .btn-primary:hover:not(:disabled) { opacity: 0.9; }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .btn-secondary { padding: 9px 18px; background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 14px; cursor: pointer; }
    .btn-secondary:hover { border-color: var(--primary); color: var(--primary); }

    .spinner { width: 14px; height: 14px; border: 2px solid rgba(255,255,255,.3); border-top-color: #fff; border-radius: 50%; animation: spin .6s linear infinite; }
    @keyframes spin { to { transform: rotate(360deg); } }

    /* Three-Way Match Modal */
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.7); z-index: 300; display: flex; align-items: center; justify-content: center; padding: 24px; }
    .modal-card { background: var(--surface); border: 1px solid var(--border); border-radius: 16px; width: 100%; max-width: 920px; max-height: 85vh; display: flex; flex-direction: column; overflow: hidden; }
    .modal-header { display: flex; justify-content: space-between; align-items: center; padding: 20px 24px; border-bottom: 1px solid var(--border); }
    .modal-title-area { display: flex; align-items: center; gap: 12px; }
    .modal-title-area h2 { font-size: 18px; font-weight: 700; margin: 0; }
    .match-badge { padding: 4px 12px; border-radius: 20px; font-size: 13px; font-weight: 600; }
    .match-badge.matched { background: rgba(52,211,153,.15); color: #34d399; border: 1px solid rgba(52,211,153,.3); }
    .match-badge.disputed { background: rgba(239,68,68,.15); color: #ef4444; border: 1px solid rgba(239,68,68,.3); }

    .match-summary { padding: 12px 24px; font-size: 14px; color: var(--text-secondary); background: var(--surface-2); }

    .match-table-wrapper { flex: 1; overflow-y: auto; padding: 20px 24px; }
    .match-table { width: 100%; border-collapse: collapse; font-size: 13px; }
    .match-table th { padding: 10px 12px; text-align: left; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: var(--text-secondary); border-bottom: 1px solid var(--border); background: var(--surface-2); }
    .match-table th.num-col, .match-table td.num-col { text-align: right; }
    .match-table th.center-col, .match-table td.center-col { text-align: center; }
    .match-table td { padding: 10px 12px; border-bottom: 1px solid var(--border); color: var(--text-primary); }
    .match-table tr:last-child td { border-bottom: none; }
    .row-matched { background: rgba(52,211,153,.04); }
    .row-disputed { background: rgba(239,68,68,.04); }
    .variance-warn { color: var(--danger) !important; font-weight: 600; }
    .match-icon { font-size: 16px; font-weight: 700; }
    .match-icon.matched { color: #34d399; }
    .match-icon.disputed { color: #ef4444; cursor: help; }
    .mono { font-family: 'SF Mono', monospace; font-size: 12px; }
    .discrepancy-row td { padding: 4px 12px 8px; }
    .discrepancy-note { font-size: 12px; color: var(--danger); background: rgba(239,68,68,.08); padding: 4px 10px; border-radius: 4px; display: inline-block; }
    .modal-footer { padding: 16px 24px; border-top: 1px solid var(--border); display: flex; justify-content: flex-end; }
  `]
})
export class InvoicesComponent implements OnInit {
  private invoiceService = inject(InvoiceService);
  private fb = inject(FormBuilder);

  invoices = signal<InvoiceDto[]>([]);
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
  matchResult = signal<ThreeWayMatchResultDto | null>(null);

  private searchSubject = new Subject<string>();
  readonly pageSize = 10;

  statusChips = [
    { label: 'All', value: null },
    { label: 'Pending', value: 0 },
    { label: 'Matched', value: 1 },
    { label: 'Disputed', value: 3 },
    { label: 'Paid', value: 5 },
  ];

  invForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadInvoices();
    this.searchSubject.pipe(debounceTime(350), distinctUntilChanged())
      .subscribe(term => {
        this.searchTerm.set(term);
        this.currentPage.set(1);
        this.loadInvoices();
      });
  }

  initForm(): void {
    this.invForm = this.fb.group({
      invoiceNumber: ['', Validators.required],
      purchaseOrderId: ['', Validators.required],
      invoiceDate: ['', Validators.required],
      dueDate: ['', Validators.required],
      lineItems: this.fb.array([this.createInvLine()])
    });
  }

  createInvLine() {
    return this.fb.group({
      itemDescription: ['', Validators.required],
      itemCode: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0.01)]],
      currency: ['USD']
    });
  }

  get invLineControls() {
    return (this.invForm.get('lineItems') as FormArray).controls as FormGroup[];
  }

  addInvLine(): void {
    (this.invForm.get('lineItems') as FormArray).push(this.createInvLine());
  }

  removeInvLine(index: number): void {
    (this.invForm.get('lineItems') as FormArray).removeAt(index);
  }

  loadInvoices(): void {
    this.loading.set(true);
    this.invoiceService.getInvoices(
      this.currentPage(), this.pageSize,
      this.searchTerm() || undefined,
      this.selectedStatus() ?? undefined
    ).subscribe({
      next: (result) => {
        this.invoices.set(result.items);
        this.totalCount.set(result.totalCount);
        this.hasNextPage.set(result.hasNextPage);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(event: Event): void {
    this.searchSubject.next((event.target as HTMLInputElement).value);
  }

  setStatus(status: number | null): void {
    this.selectedStatus.set(status);
    this.currentPage.set(1);
    this.loadInvoices();
  }

  toggleMenu(id: string): void {
    this.activeMenu.set(this.activeMenu() === id ? null : id);
  }

  closeMenu(): void { this.activeMenu.set(null); }

  runMatch(id: string): void {
    this.invoiceService.performMatch(id).subscribe({
      next: (result) => {
        this.matchResult.set(result);
        this.loadInvoices();
      }
    });
  }

  viewMatchResult(id: string): void {
    this.invoiceService.performMatch(id).subscribe({
      next: (result) => this.matchResult.set(result)
    });
  }

  closeMatchModal(): void { this.matchResult.set(null); }

  openCreateDrawer(): void {
    this.initForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  submitInvoice(): void {
    if (this.invForm.invalid) return;
    this.submitting.set(true);
    this.formError.set(null);

    const v = this.invForm.value;
    const request: CreateInvoiceRequest = {
      invoiceNumber: v.invoiceNumber,
      purchaseOrderId: v.purchaseOrderId,
      invoiceDate: v.invoiceDate,
      dueDate: v.dueDate,
      lineItems: v.lineItems
    };

    this.invoiceService.createInvoice(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeDrawer();
        this.loadInvoices();
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(err.error?.detail ?? err.error?.error ?? 'Failed to record invoice.');
      }
    });
  }

  getStatusLabel(status: number): string { return INVOICE_STATUS_LABELS[status] ?? 'Unknown'; }
  getStatusClass(status: number): string { return INVOICE_STATUS_CLASSES[status] ?? ''; }
  isOverdue(date: string, status: number): boolean {
    if (status >= 4) return false;
    return new Date(date) < new Date();
  }

  paginationStart(): number { return (this.currentPage() - 1) * this.pageSize + 1; }
  paginationEnd(): number { return Math.min(this.currentPage() * this.pageSize, this.totalCount()); }
  prevPage(): void { if (this.currentPage() > 1) { this.currentPage.update(p => p - 1); this.loadInvoices(); } }
  nextPage(): void { if (this.hasNextPage()) { this.currentPage.update(p => p + 1); this.loadInvoices(); } }
}
