import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface BudgetAllocationDto {
  id: string;
  department: string;
  category: string;
  period: number; // 0=Monthly,1=Quarterly,2=Annual
  fiscalYear: number;
  fiscalQuarter: number;
  fiscalMonth: number;
  allocatedAmount: number;
  committedAmount: number;
  spentAmount: number;
  availableAmount: number;
  utilizationPercent: number;
  isOverBudget: boolean;
  currency: string;
}

export interface AllocateBudgetRequest {
  department: string;
  category: string;
  period: number;
  fiscalYear: number;
  fiscalQuarter: number;
  fiscalMonth: number;
  allocatedAmount: number;
  currency: string;
}

export const BUDGET_PERIOD_LABELS: Record<number, string> = {
  0: 'Monthly',
  1: 'Quarterly',
  2: 'Annual'
};

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private readonly apiUrl = 'http://localhost:5000/api/budgets';

  constructor(private http: HttpClient) {}

  getBudgets(fiscalYear: number, period?: number, department?: string):
    Observable<BudgetAllocationDto[]> {
    let params = new HttpParams().set('fiscalYear', fiscalYear);
    if (period !== undefined && period !== null) params = params.set('period', period);
    if (department) params = params.set('department', department);
    return this.http.get<BudgetAllocationDto[]>(this.apiUrl, { params });
  }

  allocateBudget(request: AllocateBudgetRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request);
  }
}
