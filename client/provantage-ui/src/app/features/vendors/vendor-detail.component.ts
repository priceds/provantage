import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { forkJoin } from 'rxjs';
import {
  AnalyticsService,
  VendorPerformanceDto
} from '../../core/services/analytics.service';
import {
  CONTRACT_STATUS_CLASSES,
  CONTRACT_STATUS_LABELS,
  ContractDto,
  ContractsService
} from '../../core/services/contracts.service';
import {
  PO_STATUS_CLASSES,
  PO_STATUS_LABELS,
  PurchaseOrderDto,
  PurchaseOrderService
} from '../../core/services/purchase-order.service';
import {
  VENDOR_STATUS_CLASSES,
  VENDOR_STATUS_LABELS,
  VendorDetailDto,
  VendorService
} from '../../core/services/vendor.service';

@Component({
  selector: 'app-vendor-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatTabsModule],
  template: `
    <div class="page-container">
      <a routerLink="/vendors" class="back-link">
        <mat-icon>arrow_back</mat-icon>
        Back to Vendors
      </a>

      @if (loading()) {
        <div class="loading-shell glass-card">Loading vendor profile…</div>
      } @else {
        @if (vendor(); as details) {
          <div class="hero-card glass-card">
            <div class="hero-copy">
              <div class="vendor-meta">
                <div class="vendor-avatar">{{ initials(details.companyName) }}</div>
                <div>
                  <h1 class="hero-title">{{ details.companyName }}</h1>
                  <div class="hero-subtitle">{{ details.category }} · {{ details.email }}</div>
                </div>
              </div>
              <div class="hero-tags">
                <span class="status-badge" [ngClass]="vendorStatusClass(details.status)">
                  {{ vendorStatusLabel(details.status) }}
                </span>
                <span class="rating-pill">
                  <mat-icon>star</mat-icon>
                  {{ details.rating | number:'1.1-1' }}
                </span>
              </div>
            </div>

            <div class="hero-actions">
              @if (details.status === 0) {
                <button class="primary-btn" type="button" (click)="changeStatus(1)">Approve</button>
              }
              @if (details.status === 1) {
                <button class="secondary-btn" type="button" (click)="changeStatus(2)">Suspend</button>
                <button class="danger-btn" type="button" (click)="changeStatus(3)">Blacklist</button>
              }
              @if (details.status === 2) {
                <button class="primary-btn" type="button" (click)="changeStatus(1)">Re-Approve</button>
              }
            </div>
          </div>

          <mat-tab-group class="detail-tabs">
            <mat-tab label="Info">
              <div class="tab-grid">
                <div class="glass-card info-card">
                  <h3>Company Overview</h3>
                  <div class="info-row"><span>Email</span><strong>{{ details.email }}</strong></div>
                  <div class="info-row"><span>Phone</span><strong>{{ details.phone }}</strong></div>
                  <div class="info-row"><span>Payment Terms</span><strong>{{ details.paymentTerms }}</strong></div>
                  <div class="info-row"><span>Tax ID</span><strong>{{ details.taxId || 'Not provided' }}</strong></div>
                  <div class="info-row"><span>Website</span><strong>{{ details.website || 'Not provided' }}</strong></div>
                  <div class="info-row"><span>Created By</span><strong>{{ details.createdBy || 'System' }}</strong></div>
                </div>

                <div class="glass-card info-card">
                  <h3>Address</h3>
                  <p class="address-line">{{ details.address.street || 'Street not provided' }}</p>
                  <p class="address-line">{{ details.address.city }}, {{ details.address.state }}</p>
                  <p class="address-line">{{ details.address.postalCode }} · {{ details.address.country }}</p>
                  @if (details.statusNotes) {
                    <div class="notes-block">
                      <div class="notes-title">Status Notes</div>
                      <p>{{ details.statusNotes }}</p>
                    </div>
                  }
                </div>
              </div>

              <div class="glass-card contact-card">
                <div class="section-head">
                  <h3>Contacts</h3>
                </div>

                @if (details.contacts.length === 0) {
                  <p class="empty-copy">No contacts have been added for this vendor yet.</p>
                } @else {
                  <table class="compact-table">
                    <thead>
                      <tr>
                        <th>Name</th>
                        <th>Role</th>
                        <th>Email</th>
                        <th>Phone</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (contact of details.contacts; track contact.id) {
                        <tr>
                          <td>{{ contact.name }}</td>
                          <td>{{ contact.jobTitle }}</td>
                          <td>{{ contact.email }}</td>
                          <td>{{ contact.phone }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                }
              </div>
            </mat-tab>

            <mat-tab label="Contracts">
              <div class="glass-card list-card">
                <div class="section-head">
                  <h3>Contracts</h3>
                  <button class="primary-btn" type="button" (click)="openContractCreate()">New Contract</button>
                </div>

                @if (contracts().length === 0) {
                  <p class="empty-copy">No contracts are linked to this vendor yet.</p>
                } @else {
                  <table class="compact-table">
                    <thead>
                      <tr>
                        <th>Contract</th>
                        <th>Value</th>
                        <th>End Date</th>
                        <th>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (contract of contracts(); track contract.id) {
                        <tr>
                          <td>
                            <div class="table-primary">{{ contract.contractNumber }}</div>
                            <div class="table-secondary">{{ contract.title }}</div>
                          </td>
                          <td>{{ contract.value | currency:contract.currency:'symbol':'1.0-0' }}</td>
                          <td>{{ contract.endDate | date:'mediumDate' }}</td>
                          <td>
                            <span class="status-badge" [ngClass]="contractStatusClass(contract.status)">
                              {{ contractStatusLabel(contract.status) }}
                            </span>
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                }
              </div>
            </mat-tab>

            <mat-tab label="Orders">
              <div class="glass-card list-card">
                <div class="section-head">
                  <h3>Purchase Orders</h3>
                </div>

                @if (orders().length === 0) {
                  <p class="empty-copy">No purchase orders have been issued to this vendor yet.</p>
                } @else {
                  <table class="compact-table">
                    <thead>
                      <tr>
                        <th>PO Number</th>
                        <th>Expected Delivery</th>
                        <th>Total</th>
                        <th>Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (order of orders(); track order.id) {
                        <tr>
                          <td>{{ order.orderNumber }}</td>
                          <td>{{ order.expectedDeliveryDate | date:'mediumDate' }}</td>
                          <td>{{ order.totalAmount | currency:order.currency:'symbol':'1.0-0' }}</td>
                          <td>
                            <span class="status-badge" [ngClass]="orderStatusClass(order.status)">
                              {{ orderStatusLabel(order.status) }}
                            </span>
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                }
              </div>
            </mat-tab>

            <mat-tab label="Performance">
              <div class="performance-grid">
                <div class="glass-card metric-card">
                  <span class="metric-label">On-Time Delivery</span>
                  <strong class="metric-value">{{ performance()?.onTimeDeliveryRate ?? 0 | number:'1.0-1' }}%</strong>
                </div>
                <div class="glass-card metric-card">
                  <span class="metric-label">Invoice Match Rate</span>
                  <strong class="metric-value">{{ performance()?.invoiceMatchRate ?? 0 | number:'1.0-1' }}%</strong>
                </div>
                <div class="glass-card metric-card">
                  <span class="metric-label">Total Orders</span>
                  <strong class="metric-value">{{ performance()?.totalOrders ?? 0 }}</strong>
                </div>
                <div class="glass-card metric-card">
                  <span class="metric-label">Total Spend</span>
                  <strong class="metric-value">
                    {{ performance()?.totalSpend ?? 0 | currency:(performance()?.currency ?? 'USD'):'symbol':'1.0-0' }}
                  </strong>
                </div>
              </div>
            </mat-tab>
          </mat-tab-group>
        } @else {
          <div class="loading-shell glass-card">Vendor not found.</div>
        }
      }
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .back-link {
      display: inline-flex;
      align-items: center;
      gap: $space-xs;
      color: $text-secondary;
      margin-bottom: $space-lg;
      text-decoration: none;
      &:hover { color: $text-primary; }
    }

    .loading-shell {
      display: grid;
      place-items: center;
      min-height: 240px;
      color: $text-secondary;
    }

    .hero-card,
    .hero-copy,
    .vendor-meta,
    .hero-tags,
    .hero-actions,
    .section-head,
    .info-row {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: $space-lg;
    }

    .hero-card {
      padding: $space-xl;
      margin-bottom: $space-xl;
      align-items: flex-start;
    }

    .hero-copy {
      flex: 1;
      align-items: flex-start;
      flex-direction: column;
    }

    .vendor-meta { justify-content: flex-start; }

    .vendor-avatar {
      width: 58px;
      height: 58px;
      border-radius: $radius-lg;
      background: linear-gradient(135deg, $color-primary, $color-accent);
      color: white;
      display: grid;
      place-items: center;
      font-size: $text-lg;
      font-weight: 700;
      flex-shrink: 0;
    }

    .hero-title { color: $text-primary; font-size: $text-3xl; margin-bottom: $space-xs; }
    .hero-subtitle { color: $text-secondary; font-size: $text-sm; }

    .hero-tags { justify-content: flex-start; }

    .rating-pill {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 4px $space-sm;
      border-radius: $radius-pill;
      background: rgba($color-warning, 0.15);
      color: $color-warning;
      font-size: $text-xs;
      font-weight: 700;
    }

    .primary-btn,
    .secondary-btn,
    .danger-btn {
      padding: $space-sm $space-md;
      border-radius: $radius-md;
      border: 1px solid transparent;
      cursor: pointer;
      font-size: $text-sm;
      font-weight: 600;
    }

    .primary-btn {
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 10%));
      color: white;
    }

    .secondary-btn {
      background: transparent;
      color: $text-secondary;
      border-color: $border-subtle;
    }

    .danger-btn {
      background: rgba($color-danger, 0.12);
      color: $color-danger;
    }

    .detail-tabs { --mdc-tab-indicator-active-indicator-color: #{$color-primary}; }

    .tab-grid,
    .performance-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: $space-lg;
      margin-top: $space-lg;
    }

    .performance-grid { grid-template-columns: repeat(4, minmax(0, 1fr)); }

    .info-card,
    .contact-card,
    .list-card,
    .metric-card {
      padding: $space-xl;
      margin-top: $space-lg;
    }

    h3 { color: $text-primary; margin-bottom: $space-lg; }

    .info-row {
      padding: $space-sm 0;
      border-bottom: 1px solid rgba(255,255,255,0.05);
      span { color: $text-secondary; font-size: $text-sm; }
      strong { color: $text-primary; font-size: $text-sm; }
    }

    .address-line { color: $text-secondary; margin-bottom: $space-sm; }
    .notes-block {
      margin-top: $space-lg;
      padding: $space-md;
      border-radius: $radius-md;
      background: rgba($color-warning, 0.08);
      border: 1px solid rgba($color-warning, 0.18);
    }
    .notes-title { color: $text-primary; font-weight: 600; margin-bottom: $space-xs; }

    .compact-table { width: 100%; border-collapse: collapse; }
    .compact-table th,
    .compact-table td {
      padding: $space-md 0;
      text-align: left;
      border-bottom: 1px solid rgba(255,255,255,0.05);
    }

    .compact-table th {
      color: $text-muted;
      font-size: $text-xs;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .compact-table td { color: $text-secondary; font-size: $text-sm; }
    .table-primary { color: $text-primary; font-weight: 600; }
    .table-secondary { color: $text-secondary; font-size: $text-xs; }

    .metric-card {
      display: flex;
      flex-direction: column;
      gap: $space-sm;
      justify-content: center;
      min-height: 140px;
    }

    .metric-label { color: $text-secondary; font-size: $text-sm; }
    .metric-value { color: $text-primary; font-size: $text-2xl; }

    .empty-copy { color: $text-secondary; font-size: $text-sm; }

    .status-approved,
    .status-active { background: rgba($color-accent, 0.15); color: $color-accent; }
    .status-pending,
    .status-warning { background: rgba($color-warning, 0.15); color: $color-warning; }
    .status-danger { background: rgba($color-danger, 0.15); color: $color-danger; }
    .status-info { background: rgba($color-info, 0.15); color: $color-info; }
    .status-muted { background: rgba(255,255,255,0.08); color: $text-secondary; }
    .status-draft { background: rgba($color-primary, 0.15); color: $color-primary; }
  `]
})
export class VendorDetailComponent implements OnInit {
  vendor = signal<VendorDetailDto | null>(null);
  contracts = signal<ContractDto[]>([]);
  orders = signal<PurchaseOrderDto[]>([]);
  performance = signal<VendorPerformanceDto | null>(null);
  loading = signal(true);

  private vendorId = '';
  private readonly currentYear = new Date().getFullYear();

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private vendorService: VendorService,
    private contractsService: ContractsService,
    private purchaseOrderService: PurchaseOrderService,
    private analyticsService: AnalyticsService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading.set(false);
      return;
    }

    this.vendorId = id;
    this.loadData();
  }

  loadData(): void {
    this.loading.set(true);

    forkJoin({
      vendor: this.vendorService.getVendor(this.vendorId),
      contracts: this.contractsService.getAll(1, 10, { vendorId: this.vendorId }),
      orders: this.purchaseOrderService.getPurchaseOrders(1, 10, undefined, undefined, this.vendorId),
      performance: this.analyticsService.getVendorPerformance(this.currentYear)
    }).subscribe({
      next: ({ vendor, contracts, orders, performance }) => {
        this.vendor.set(vendor);
        this.contracts.set(contracts.items);
        this.orders.set(orders.items);
        this.performance.set(performance.find(item => item.vendorId === this.vendorId) ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  changeStatus(status: number): void {
    this.vendorService.changeStatus(this.vendorId, status).subscribe(() => this.loadData());
  }

  openContractCreate(): void {
    this.router.navigate(['/contracts'], { queryParams: { vendorId: this.vendorId, open: true } });
  }

  initials(name: string): string {
    return name.split(' ').map(part => part[0]).slice(0, 2).join('').toUpperCase();
  }

  vendorStatusLabel(status: number): string {
    return VENDOR_STATUS_LABELS[status] ?? 'Unknown';
  }

  vendorStatusClass(status: number): string {
    return VENDOR_STATUS_CLASSES[status] ?? 'status-muted';
  }

  contractStatusLabel(status: number): string {
    return CONTRACT_STATUS_LABELS[status] ?? 'Unknown';
  }

  contractStatusClass(status: number): string {
    return CONTRACT_STATUS_CLASSES[status] ?? 'status-muted';
  }

  orderStatusLabel(status: number): string {
    return PO_STATUS_LABELS[status] ?? 'Unknown';
  }

  orderStatusClass(status: number): string {
    return PO_STATUS_CLASSES[status] ?? 'status-muted';
  }
}
