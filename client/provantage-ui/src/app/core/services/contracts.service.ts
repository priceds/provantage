import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedList } from './vendor.service';

export interface ContractDto {
  id: string;
  contractNumber: string;
  vendorId: string;
  vendorName: string;
  title: string;
  status: number;
  startDate: string;
  endDate: string;
  value: number;
  currency: string;
  daysRemaining: number;
  createdAt: string;
}

export interface CreateContractRequest {
  vendorId: string;
  title: string;
  startDate: string;
  endDate: string;
  value: number;
  currency: string;
}

export interface ContractFilters {
  status?: number;
  vendorId?: string;
  expiringWithin30Days?: boolean;
}

export const CONTRACT_STATUS_LABELS: Record<number, string> = {
  0: 'Draft',
  1: 'Active',
  2: 'Expiring',
  3: 'Expired',
  4: 'Terminated',
  5: 'Renewed'
};

export const CONTRACT_STATUS_CLASSES: Record<number, string> = {
  0: 'status-draft',
  1: 'status-active',
  2: 'status-warning',
  3: 'status-danger',
  4: 'status-muted',
  5: 'status-info'
};

@Injectable({ providedIn: 'root' })
export class ContractsService {
  private readonly apiUrl = '/api/contracts';

  constructor(private http: HttpClient) {}

  getAll(
    page = 1,
    pageSize = 20,
    filters: ContractFilters = {}
  ): Observable<PaginatedList<ContractDto>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);

    if (filters.status !== undefined && filters.status !== null) {
      params = params.set('status', filters.status);
    }

    if (filters.vendorId) {
      params = params.set('vendorId', filters.vendorId);
    }

    if (filters.expiringWithin30Days) {
      params = params.set('expiringWithin30Days', true);
    }

    return this.http.get<PaginatedList<ContractDto>>(this.apiUrl, { params });
  }

  getById(id: string): Observable<ContractDto> {
    return this.http.get<ContractDto>(`${this.apiUrl}/${id}`);
  }

  getExpiring(daysAhead = 30): Observable<ContractDto[]> {
    return this.http.get<ContractDto[]>(`${this.apiUrl}/expiring?daysAhead=${daysAhead}`);
  }

  create(payload: CreateContractRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, payload);
  }
}
