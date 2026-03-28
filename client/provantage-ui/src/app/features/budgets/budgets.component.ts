import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  BudgetService, BudgetAllocationDto, AllocateBudgetRequest, BUDGET_PERIOD_LABELS
} from '../../core/services/budget.service';

@Component({
  selector: 'app-budgets',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
<div class="page-container">
  <!-- Header -->
  <div class="page-header">
    <div class="header-left">
      <h1 class="page-title">Budget Management</h1>
      <p class="page-subtitle">Track department spend allocations, commitments and utilization</p>
    </div>
    <div class="header-actions">
      <select class="year-select" [value]="selectedYear()" (change)="changeYear($event)">
        @for (y of availableYears; track y) {
          <option [value]="y">{{y}}</option>
        }
      </select>
      <button class="btn-primary" (click)="openDrawer()">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
          <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        Allocate Budget
      </button>
    </div>
  </div>

  <!-- Summary Cards -->
  @if (!loading() && budgets().length > 0) {
    <div class="summary-grid">
      <div class="summary-card">
        <div class="card-label">Total Allocated</div>
        <div class="card-value primary">{{totalAllocated() | number:'1.0-0'}}</div>
        <div class="card-currency">{{budgets()[0]?.currency ?? 'USD'}}</div>
      </div>
      <div class="summary-card">
        <div class="card-label">Committed (POs)</div>
        <div class="card-value warning">{{totalCommitted() | number:'1.0-0'}}</div>
        <div class="card-currency">{{budgets()[0]?.currency ?? 'USD'}}</div>
      </div>
      <div class="summary-card">
        <div class="card-label">Spent (Invoices)</div>
        <div class="card-value accent">{{totalSpent() | number:'1.0-0'}}</div>
        <div class="card-currency">{{budgets()[0]?.currency ?? 'USD'}}</div>
      </div>
      <div class="summary-card">
        <div class="card-label">Available</div>
        <div class="card-value" [class.danger]="totalAvailable() < 0">
          {{totalAvailable() | number:'1.0-0'}}
        </div>
        <div class="card-currency">{{budgets()[0]?.currency ?? 'USD'}}</div>
      </div>
    </div>
  }

  <!-- Budget Table -->
  <div class="table-card">
    @if (loading()) {
      <div class="skeleton-rows">
        @for (i of [1,2,3,4,5]; track i) {
          <div class="skeleton-row">
            <div class="skeleton-cell wide shimmer"></div>
            <div class="skeleton-cell shimmer"></div>
            <div class="skeleton-cell shimmer"></div>
            <div class="skeleton-cell shimmer"></div>
            <div class="skeleton-cell wide shimmer"></div>
          </div>
        }
      </div>
    } @else if (budgets().length === 0) {
      <div class="empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" opacity="0.3">
          <rect x="2" y="3" width="20" height="14" rx="2"/><line x1="8" y1="21" x2="16" y2="21"/>
          <line x1="12" y1="17" x2="12" y2="21"/>
        </svg>
        <p>No budgets allocated for {{selectedYear()}}</p>
        <button class="btn-primary" (click)="openDrawer()">Allocate First Budget</button>
      </div>
    } @else {
      <table class="data-table">
        <thead>
          <tr>
            <th>Department</th>
            <th>Category</th>
            <th>Period</th>
            <th class="num-col">Allocated</th>
            <th class="num-col">Committed</th>
            <th class="num-col">Spent</th>
            <th class="num-col">Available</th>
            <th>Utilization</th>
          </tr>
        </thead>
        <tbody>
          @for (b of budgets(); track b.id) {
            <tr [class.over-budget]="b.isOverBudget">
              <td><span class="dept-badge">{{b.department}}</span></td>
              <td>{{b.category}}</td>
              <td>
                <span class="period-tag">{{getPeriodLabel(b.period)}}</span>
              </td>
              <td class="num-col amount">{{b.currency}} {{b.allocatedAmount | number:'1.0-0'}}</td>
              <td class="num-col committed">{{b.currency}} {{b.committedAmount | number:'1.0-0'}}</td>
              <td class="num-col spent">{{b.currency}} {{b.spentAmount | number:'1.0-0'}}</td>
              <td class="num-col" [class.negative]="b.availableAmount < 0">
                {{b.currency}} {{b.availableAmount | number:'1.0-0'}}
              </td>
              <td>
                <div class="utilization-bar-wrapper">
                  <div class="utilization-bar">
                    <div class="utilization-fill"
                      [style.width.%]="Math.min(b.utilizationPercent, 100)"
                      [class.fill-warning]="b.utilizationPercent > 75 && b.utilizationPercent <= 90"
                      [class.fill-danger]="b.utilizationPercent > 90">
                    </div>
                  </div>
                  <span class="utilization-pct" [class.pct-danger]="b.isOverBudget">
                    {{b.utilizationPercent | number:'1.0-0'}}%
                  </span>
                </div>
              </td>
            </tr>
          }
        </tbody>
      </table>
    }
  </div>
</div>

<!-- Allocate Budget Drawer -->
@if (drawerOpen()) {
  <div class="drawer-overlay" (click)="closeDrawer()"></div>
  <div class="drawer drawer-open">
    <div class="drawer-header">
      <h2>Allocate Budget</h2>
      <button class="close-btn" (click)="closeDrawer()">✕</button>
    </div>
    <form [formGroup]="budgetForm" (ngSubmit)="submitBudget()" class="drawer-body">
      @if (formError()) {
        <div class="error-banner">{{formError()}}</div>
      }
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Department <span class="required">*</span></label>
          <input type="text" class="form-control" formControlName="department" placeholder="e.g. Engineering"/>
        </div>
        <div class="form-group">
          <label class="form-label">Category <span class="required">*</span></label>
          <input type="text" class="form-control" formControlName="category" placeholder="e.g. Software"/>
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Period <span class="required">*</span></label>
          <select class="form-control" formControlName="period">
            <option value="2">Annual</option>
            <option value="1">Quarterly</option>
            <option value="0">Monthly</option>
          </select>
        </div>
        <div class="form-group">
          <label class="form-label">Fiscal Year <span class="required">*</span></label>
          <input type="number" class="form-control" formControlName="fiscalYear" [value]="selectedYear()"/>
        </div>
      </div>
      @if (budgetForm.get('period')?.value == 1) {
        <div class="form-group">
          <label class="form-label">Quarter <span class="required">*</span></label>
          <select class="form-control" formControlName="fiscalQuarter">
            <option value="1">Q1 (Jan–Mar)</option>
            <option value="2">Q2 (Apr–Jun)</option>
            <option value="3">Q3 (Jul–Sep)</option>
            <option value="4">Q4 (Oct–Dec)</option>
          </select>
        </div>
      }
      @if (budgetForm.get('period')?.value == 0) {
        <div class="form-group">
          <label class="form-label">Month <span class="required">*</span></label>
          <select class="form-control" formControlName="fiscalMonth">
            @for (m of months; track m.value) {
              <option [value]="m.value">{{m.label}}</option>
            }
          </select>
        </div>
      }
      <div class="form-row">
        <div class="form-group">
          <label class="form-label">Allocated Amount <span class="required">*</span></label>
          <input type="number" class="form-control" formControlName="allocatedAmount" placeholder="0.00" min="1" step="0.01"/>
        </div>
        <div class="form-group">
          <label class="form-label">Currency</label>
          <select class="form-control" formControlName="currency">
            <option>USD</option><option>EUR</option><option>GBP</option><option>INR</option>
          </select>
        </div>
      </div>

      <div class="drawer-footer">
        <button type="button" class="btn-secondary" (click)="closeDrawer()">Cancel</button>
        <button type="submit" class="btn-primary" [disabled]="budgetForm.invalid || submitting()">
          @if (submitting()) {<span class="spinner"></span>} Save Budget
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
    .header-actions { display: flex; gap: 12px; align-items: center; }
    .year-select { padding: 8px 12px; background: var(--surface-2); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 14px; cursor: pointer; }

    .summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px; }
    .summary-card { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; padding: 20px; }
    .card-label { font-size: 12px; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
    .card-value { font-size: 28px; font-weight: 700; color: var(--text-primary); line-height: 1; }
    .card-value.primary { color: var(--primary); }
    .card-value.warning { color: #f59e0b; }
    .card-value.accent { color: var(--accent); }
    .card-value.danger { color: var(--danger); }
    .card-currency { font-size: 12px; color: var(--text-secondary); margin-top: 4px; }

    .table-card { background: var(--surface); border: 1px solid var(--border); border-radius: 12px; overflow: hidden; }
    .data-table { width: 100%; border-collapse: collapse; }
    .data-table th { padding: 12px 16px; text-align: left; font-size: 12px; font-weight: 600; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.5px; border-bottom: 1px solid var(--border); background: var(--surface-2); }
    .data-table th.num-col { text-align: right; }
    .data-table td { padding: 14px 16px; border-bottom: 1px solid var(--border); font-size: 14px; color: var(--text-primary); vertical-align: middle; }
    .data-table td.num-col { text-align: right; }
    .data-table tbody tr:last-child td { border-bottom: none; }
    .data-table tbody tr:hover { background: var(--surface-2); }
    .data-table tbody tr.over-budget { background: rgba(239,68,68,.04); }

    .dept-badge { display: inline-block; padding: 3px 10px; background: rgba(99,102,241,.12); color: #818cf8; border-radius: 20px; font-size: 12px; font-weight: 500; }
    .period-tag { font-size: 12px; color: var(--text-secondary); }
    .amount { font-weight: 600; }
    .committed { color: #f59e0b; }
    .spent { color: var(--accent); }
    .negative { color: var(--danger) !important; font-weight: 600; }

    .utilization-bar-wrapper { display: flex; align-items: center; gap: 10px; min-width: 140px; }
    .utilization-bar { flex: 1; height: 6px; background: var(--surface-2); border-radius: 3px; overflow: hidden; }
    .utilization-fill { height: 100%; background: var(--accent); border-radius: 3px; transition: width .3s ease; }
    .utilization-fill.fill-warning { background: #f59e0b; }
    .utilization-fill.fill-danger { background: var(--danger); }
    .utilization-pct { font-size: 13px; font-weight: 500; color: var(--text-secondary); min-width: 36px; text-align: right; }
    .utilization-pct.pct-danger { color: var(--danger); }

    .empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; padding: 60px; gap: 16px; color: var(--text-secondary); }

    .skeleton-rows { padding: 12px 0; }
    .skeleton-row { display: flex; gap: 16px; padding: 14px 16px; align-items: center; border-bottom: 1px solid var(--border); }
    .skeleton-cell { height: 14px; border-radius: 4px; flex: 1; }
    .skeleton-cell.wide { flex: 2; }
    @keyframes shimmer { 0%{background-position:-200px 0} 100%{background-position:calc(200px + 100%) 0} }
    .shimmer { background: linear-gradient(90deg, var(--surface-2) 25%, var(--surface) 50%, var(--surface-2) 75%); background-size: 200px 100%; animation: shimmer 1.2s infinite; }

    /* Drawer */
    .drawer-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.6); z-index: 200; }
    .drawer { position: fixed; top: 0; right: 0; width: 520px; height: 100vh; background: var(--surface); border-left: 1px solid var(--border); z-index: 201; display: flex; flex-direction: column; overflow: hidden; }
    .drawer.drawer-open { transform: none; }
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
export class BudgetsComponent implements OnInit {
  private budgetService = inject(BudgetService);
  private fb = inject(FormBuilder);

  budgets = signal<BudgetAllocationDto[]>([]);
  loading = signal(true);
  selectedYear = signal(new Date().getFullYear());
  drawerOpen = signal(false);
  submitting = signal(false);
  formError = signal<string | null>(null);

  Math = Math;
  availableYears = [new Date().getFullYear() - 1, new Date().getFullYear(), new Date().getFullYear() + 1];

  months = [
    {value:1,label:'January'},{value:2,label:'February'},{value:3,label:'March'},
    {value:4,label:'April'},{value:5,label:'May'},{value:6,label:'June'},
    {value:7,label:'July'},{value:8,label:'August'},{value:9,label:'September'},
    {value:10,label:'October'},{value:11,label:'November'},{value:12,label:'December'}
  ];

  budgetForm!: FormGroup;

  totalAllocated = computed(() => this.budgets().reduce((s, b) => s + b.allocatedAmount, 0));
  totalCommitted = computed(() => this.budgets().reduce((s, b) => s + b.committedAmount, 0));
  totalSpent = computed(() => this.budgets().reduce((s, b) => s + b.spentAmount, 0));
  totalAvailable = computed(() => this.budgets().reduce((s, b) => s + b.availableAmount, 0));

  ngOnInit(): void {
    this.initForm();
    this.loadBudgets();
  }

  initForm(): void {
    this.budgetForm = this.fb.group({
      department: ['', Validators.required],
      category: ['', Validators.required],
      period: [2, Validators.required],
      fiscalYear: [this.selectedYear(), Validators.required],
      fiscalQuarter: [1],
      fiscalMonth: [1],
      allocatedAmount: [null, [Validators.required, Validators.min(1)]],
      currency: ['USD']
    });
  }

  loadBudgets(): void {
    this.loading.set(true);
    this.budgetService.getBudgets(this.selectedYear()).subscribe({
      next: (result) => { this.budgets.set(result); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  changeYear(event: Event): void {
    const year = Number((event.target as HTMLSelectElement).value);
    this.selectedYear.set(year);
    this.loadBudgets();
  }

  openDrawer(): void {
    this.initForm();
    this.formError.set(null);
    this.drawerOpen.set(true);
  }

  closeDrawer(): void { this.drawerOpen.set(false); }

  submitBudget(): void {
    if (this.budgetForm.invalid) return;
    this.submitting.set(true);
    this.formError.set(null);

    const v = this.budgetForm.value;
    const request: AllocateBudgetRequest = {
      department: v.department,
      category: v.category,
      period: Number(v.period),
      fiscalYear: v.fiscalYear,
      fiscalQuarter: v.fiscalQuarter ?? 1,
      fiscalMonth: v.fiscalMonth ?? 1,
      allocatedAmount: v.allocatedAmount,
      currency: v.currency
    };

    this.budgetService.allocateBudget(request).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeDrawer();
        this.loadBudgets();
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(err.error?.detail ?? err.error?.error ?? 'Failed to allocate budget.');
      }
    });
  }

  getPeriodLabel(period: number): string { return BUDGET_PERIOD_LABELS[period] ?? 'Unknown'; }
}
