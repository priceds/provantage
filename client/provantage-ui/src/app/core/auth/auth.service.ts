import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { SignalRService } from '../services/signalr.service';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  tenantId: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = '/api/auth';
  private readonly tokenKey = 'pv_access_token';
  private readonly refreshKey = 'pv_refresh_token';
  private readonly userKey = 'pv_user';

  private http = inject(HttpClient);
  private router = inject(Router);
  private signalR = inject(SignalRService);

  isAuthenticated = signal(this.hasToken());
  currentUser = signal<AuthUser | null>(this.loadStoredUser());

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        this.storeTokens(response.accessToken, response.refreshToken);
        localStorage.setItem(this.userKey, JSON.stringify(response.user));
        this.currentUser.set(response.user);
        this.isAuthenticated.set(true);
        // Connect SignalR after successful login
        this.signalR.connect(response.accessToken).catch(console.warn);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.refreshKey);
    localStorage.removeItem(this.userKey);
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.signalR.disconnect().catch(console.warn);
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  /** Re-connects SignalR on app startup if user is already authenticated. */
  tryReconnectSignalR(): void {
    const token = this.getAccessToken();
    if (token && this.isAuthenticated()) {
      this.signalR.connect(token).catch(console.warn);
    }
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(this.tokenKey);
  }

  private loadStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(this.userKey);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  }

  private storeTokens(access: string, refresh: string): void {
    localStorage.setItem(this.tokenKey, access);
    localStorage.setItem(this.refreshKey, refresh);
  }
}
