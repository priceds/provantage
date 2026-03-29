import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface VendorDto {
  id: string;
  companyName: string;
  email: string;
  phone: string;
  category: string;
  status: number; // 0=PendingApproval, 1=Approved, 2=Suspended, 3=Blacklisted
  paymentTerms: string;
  rating: number;
  city: string;
  country: string;
  createdAt: string;
}

export interface VendorDetailDto {
  id: string;
  companyName: string;
  taxId: string;
  email: string;
  phone: string;
  website: string;
  category: string;
  status: number;
  statusNotes: string;
  paymentTerms: string;
  rating: number;
  address: AddressDto;
  contacts: VendorContactDto[];
  createdAt: string;
  createdBy: string;
}

export interface AddressDto {
  street: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface VendorContactDto {
  id: string;
  name: string;
  email: string;
  phone: string;
  jobTitle: string;
  isPrimary: boolean;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CreateVendorRequest {
  companyName: string;
  taxId: string;
  email: string;
  phone: string;
  website?: string;
  category: string;
  paymentTerms: string;
  address: AddressDto;
  contacts?: { name: string; email: string; phone: string; jobTitle: string; isPrimary: boolean }[];
}

export const VENDOR_STATUS_LABELS: Record<number, string> = {
  0: 'Pending Approval',
  1: 'Approved',
  2: 'Suspended',
  3: 'Blacklisted'
};

export const VENDOR_STATUS_CLASSES: Record<number, string> = {
  0: 'status-pending',
  1: 'status-approved',
  2: 'status-warning',
  3: 'status-danger'
};

@Injectable({ providedIn: 'root' })
export class VendorService {
  private readonly apiUrl = '/api/vendors';

  constructor(private http: HttpClient) {}

  getVendors(page = 1, pageSize = 10, search?: string, status?: number, category?: string):
    Observable<PaginatedList<VendorDto>> {
    let params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status !== undefined && status !== null) params = params.set('status', status);
    if (category) params = params.set('category', category);
    return this.http.get<PaginatedList<VendorDto>>(this.apiUrl, { params });
  }

  getVendor(id: string): Observable<VendorDetailDto> {
    return this.http.get<VendorDetailDto>(`${this.apiUrl}/${id}`);
  }

  createVendor(request: CreateVendorRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request);
  }

  changeStatus(id: string, status: number, notes?: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, { status, notes });
  }

  getCategories(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/categories`);
  }
}
