import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedList } from './vendor.service';

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  internalReference: string;
  purchaseOrderId: string;
  purchaseOrderNumber: string;
  vendorName: string;
  status: number; // 0=Pending,1=Matched,2=PartiallyMatched,3=Disputed,4=Approved,5=Paid,6=Cancelled
  invoiceDate: string;
  dueDate: string;
  totalAmount: number;
  currency: string;
  matchedAt: string | null;
  createdAt: string;
}

export interface InvoiceLineItemDto {
  id: string;
  itemDescription: string;
  itemCode: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  totalPrice: number;
}

export interface ThreeWayMatchLineResult {
  itemCode: string;
  itemDescription: string;
  invoiceQty: number;
  invoiceUnitPrice: number;
  poQty: number;
  poUnitPrice: number;
  receivedQty: number;
  priceVariancePercent: number;
  quantityVariancePercent: number;
  isPriceMatched: boolean;
  isQuantityMatched: boolean;
  isMatched: boolean;
  discrepancyNote: string | null;
}

export interface ThreeWayMatchResultDto {
  invoiceId: string;
  resultStatus: number;
  isFullyMatched: boolean;
  summary: string;
  lineResults: ThreeWayMatchLineResult[];
}

export interface InvoiceDetailDto extends InvoiceDto {
  vendorId: string;
  disputeNotes: string | null;
  paidAt: string | null;
  lineItems: InvoiceLineItemDto[];
}

export interface CreateInvoiceLineItemRequest {
  itemDescription: string;
  itemCode: string;
  quantity: number;
  unitPrice: number;
  currency: string;
}

export interface CreateInvoiceRequest {
  invoiceNumber: string;
  purchaseOrderId: string;
  invoiceDate: string;
  dueDate: string;
  lineItems: CreateInvoiceLineItemRequest[];
}

export const INVOICE_STATUS_LABELS: Record<number, string> = {
  0: 'Pending',
  1: 'Matched',
  2: 'Partially Matched',
  3: 'Disputed',
  4: 'Approved',
  5: 'Paid',
  6: 'Cancelled'
};

export const INVOICE_STATUS_CLASSES: Record<number, string> = {
  0: 'status-pending',
  1: 'status-approved',
  2: 'status-review',
  3: 'status-danger',
  4: 'status-approved',
  5: 'status-converted',
  6: 'status-muted'
};

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly apiUrl = 'http://localhost:5000/api/invoices';

  constructor(private http: HttpClient) {}

  getInvoices(page = 1, pageSize = 10, search?: string, status?: number, poId?: string):
    Observable<PaginatedList<InvoiceDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined && status !== null) params = params.set('status', status);
    if (poId) params = params.set('poId', poId);
    return this.http.get<PaginatedList<InvoiceDto>>(this.apiUrl, { params });
  }

  getInvoice(id: string): Observable<InvoiceDetailDto> {
    return this.http.get<InvoiceDetailDto>(`${this.apiUrl}/${id}`);
  }

  createInvoice(request: CreateInvoiceRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request);
  }

  performMatch(id: string): Observable<ThreeWayMatchResultDto> {
    return this.http.post<ThreeWayMatchResultDto>(`${this.apiUrl}/${id}/match`, {});
  }
}
