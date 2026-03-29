import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedList } from './vendor.service';

export interface AuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  oldValues: string | null;
  newValues: string | null;
  performedBy: string;
  timestamp: string;
}

export interface AuditLogFilters {
  entityType?: string;
  entityId?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly apiUrl = '/api/audit-logs';

  constructor(private http: HttpClient) {}

  getAll(filters: AuditLogFilters = {}): Observable<PaginatedList<AuditLogDto>> {
    let params = new HttpParams()
      .set('page', filters.page ?? 1)
      .set('pageSize', filters.pageSize ?? 20);

    if (filters.entityType) {
      params = params.set('entityType', filters.entityType);
    }

    if (filters.entityId) {
      params = params.set('entityId', filters.entityId);
    }

    if (filters.from) {
      params = params.set('from', filters.from);
    }

    if (filters.to) {
      params = params.set('to', filters.to);
    }

    return this.http.get<PaginatedList<AuditLogDto>>(this.apiUrl, { params });
  }
}
