import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedList } from './vendor.service';

export interface PurchaseOrderDto {
  id: string;
  orderNumber: string;
  vendorName: string;
  vendorId: string;
  requisitionId: string | null;
  requisitionNumber: string | null;
  status: number; // 0=Created,1=Sent,2=Acknowledged,3=PartiallyReceived,4=Received,5=Closed,6=Cancelled
  orderDate: string;
  expectedDeliveryDate: string;
  totalAmount: number;
  currency: string;
  paymentTerms: string;
  createdAt: string;
}

export interface OrderLineItemDto {
  id: string;
  itemDescription: string;
  itemCode: string;
  unitOfMeasure: string;
  quantityOrdered: number;
  quantityReceived: number;
  unitPrice: number;
  currency: string;
  totalPrice: number;
  isFullyReceived: boolean;
}

export interface PurchaseOrderDetailDto extends PurchaseOrderDto {
  shippingAddress: string;
  notes: string | null;
  lineItems: OrderLineItemDto[];
}

export interface CreateOrderLineItemRequest {
  itemDescription: string;
  itemCode: string;
  unitOfMeasure: string;
  quantityOrdered: number;
  unitPrice: number;
  currency: string;
}

export interface CreatePurchaseOrderRequest {
  requisitionId?: string;
  vendorId: string;
  expectedDeliveryDate: string;
  paymentTerms: string;
  shippingAddress: string;
  notes?: string;
  lineItems: CreateOrderLineItemRequest[];
}

export const PO_STATUS_LABELS: Record<number, string> = {
  0: 'Created',
  1: 'Sent',
  2: 'Acknowledged',
  3: 'Partially Received',
  4: 'Received',
  5: 'Closed',
  6: 'Cancelled'
};

export const PO_STATUS_CLASSES: Record<number, string> = {
  0: 'status-draft',
  1: 'status-pending',
  2: 'status-review',
  3: 'status-warning',
  4: 'status-approved',
  5: 'status-muted',
  6: 'status-danger'
};

@Injectable({ providedIn: 'root' })
export class PurchaseOrderService {
  private readonly apiUrl = 'http://localhost:5000/api/purchase-orders';

  constructor(private http: HttpClient) {}

  getPurchaseOrders(page = 1, pageSize = 10, search?: string, status?: number, vendorId?: string):
    Observable<PaginatedList<PurchaseOrderDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined && status !== null) params = params.set('status', status);
    if (vendorId) params = params.set('vendorId', vendorId);
    return this.http.get<PaginatedList<PurchaseOrderDto>>(this.apiUrl, { params });
  }

  getPurchaseOrder(id: string): Observable<PurchaseOrderDetailDto> {
    return this.http.get<PurchaseOrderDetailDto>(`${this.apiUrl}/${id}`);
  }

  createPurchaseOrder(request: CreatePurchaseOrderRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request);
  }

  updateStatus(id: string, status: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, { status });
  }
}
