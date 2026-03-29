import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SpendByCategoryDto {
  category: string;
  department: string;
  totalSpend: number;
  currency: string;
  invoiceCount: number;
}

export interface VendorPerformanceDto {
  vendorId: string;
  vendorName: string;
  totalOrders: number;
  completedOrders: number;
  onTimeDeliveryRate: number;
  totalInvoices: number;
  matchedInvoices: number;
  invoiceMatchRate: number;
  totalSpend: number;
  currency: string;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly apiUrl = '/api/analytics';

  constructor(private http: HttpClient) {}

  getSpendByCategory(year: number): Observable<SpendByCategoryDto[]> {
    return this.http.get<SpendByCategoryDto[]>(`${this.apiUrl}/spend-by-category?year=${year}`);
  }

  getVendorPerformance(year: number): Observable<VendorPerformanceDto[]> {
    return this.http.get<VendorPerformanceDto[]>(`${this.apiUrl}/vendor-performance?year=${year}`);
  }
}
