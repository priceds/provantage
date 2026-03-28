import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedList } from './vendor.service';

export interface RequisitionDto {
  id: string;
  requisitionNumber: string;
  title: string;
  department: string;
  status: number; // 0=Draft,1=Submitted,2=UnderReview,3=Approved,4=Rejected,5=Cancelled,6=ConvertedToOrder
  requestedByName: string;
  totalAmount: number;
  currency: string;
  createdAt: string;
  approvedAt: string | null;
}

export interface LineItemDto {
  id: string;
  itemDescription: string;
  itemCode: string;
  category: string;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
  currency: string;
  totalPrice: number;
}

export interface RequisitionDetailDto extends RequisitionDto {
  description: string;
  rejectionReason: string | null;
  approvedByName: string | null;
  preferredVendorId: string | null;
  preferredVendorName: string | null;
  lineItems: LineItemDto[];
  submittedAt: string | null;
}

export interface CreateLineItemRequest {
  itemDescription: string;
  itemCode: string;
  category: string;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
}

export interface CreateRequisitionRequest {
  title: string;
  description: string;
  department: string;
  preferredVendorId?: string;
  currency: string;
  lineItems: CreateLineItemRequest[];
}

export const REQUISITION_STATUS_LABELS: Record<number, string> = {
  0: 'Draft',
  1: 'Submitted',
  2: 'Under Review',
  3: 'Approved',
  4: 'Rejected',
  5: 'Cancelled',
  6: 'Converted to PO'
};

export const REQUISITION_STATUS_CLASSES: Record<number, string> = {
  0: 'status-draft',
  1: 'status-pending',
  2: 'status-review',
  3: 'status-approved',
  4: 'status-danger',
  5: 'status-muted',
  6: 'status-converted'
};

@Injectable({ providedIn: 'root' })
export class RequisitionService {
  private readonly apiUrl = 'http://localhost:5000/api/requisitions';

  constructor(private http: HttpClient) {}

  getRequisitions(page = 1, pageSize = 10, search?: string, status?: number, department?: string):
    Observable<PaginatedList<RequisitionDto>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined && status !== null) params = params.set('status', status);
    if (department) params = params.set('department', department);
    return this.http.get<PaginatedList<RequisitionDto>>(this.apiUrl, { params });
  }

  getRequisition(id: string): Observable<RequisitionDetailDto> {
    return this.http.get<RequisitionDetailDto>(`${this.apiUrl}/${id}`);
  }

  createRequisition(request: CreateRequisitionRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request);
  }

  submitRequisition(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/submit`, {});
  }

  approveRequisition(id: string, comments?: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve`, { comments });
  }

  rejectRequisition(id: string, reason: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, { reason });
  }
}
