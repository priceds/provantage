import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SpendDataPoint { month: string; amount: number; currency: string; }
export interface PendingApprovalItem { id: string; number: string; title: string; department: string; amount: number; currency: string | null; submittedAt: string; }
export interface RecentActivityItem { id: string; text: string; timeAgo: string; color: string; icon: string; timestamp: string; }

export interface DashboardKpis {
  totalSpendMtd: number;
  currency: string;
  spendChangePct: number;
  openPurchaseOrders: number;
  pendingApprovals: number;
  activeVendors: number;
  budgetUtilizationAvg: number;
  spendTrend: SpendDataPoint[];
  pendingApprovalsList: PendingApprovalItem[];
  recentActivity: RecentActivityItem[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private http: HttpClient) {}

  getKpis(): Observable<DashboardKpis> {
    return this.http.get<DashboardKpis>('/api/dashboard/kpis');
  }
}
