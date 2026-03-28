import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, MatIconModule],
  template: `
    <div class="login-page">
      <div class="login-bg">
        <div class="bg-orb orb-1"></div>
        <div class="bg-orb orb-2"></div>
        <div class="bg-orb orb-3"></div>
      </div>

      <div class="login-card glass-card animate-fade-in-up">
        <!-- Logo -->
        <div class="logo-block">
          <div class="logo-icon">P</div>
          <span class="logo-text">ProVantage</span>
        </div>

        <h1 class="card-title">Welcome back</h1>
        <p class="card-subtitle">Sign in to your procurement workspace</p>

        <!-- Error -->
        @if (errorMessage()) {
          <div class="error-banner">
            <mat-icon>error_outline</mat-icon>
            {{ errorMessage() }}
          </div>
        }

        <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>
          <div class="field-group">
            <label for="email" class="field-label">Email address</label>
            <div class="input-wrapper" [class.error]="isFieldInvalid('email')">
              <mat-icon class="input-icon">mail_outline</mat-icon>
              <input
                id="email"
                type="email"
                class="field-input"
                formControlName="email"
                placeholder="you@company.com"
                autocomplete="email" />
            </div>
            @if (isFieldInvalid('email')) {
              <span class="field-error">Please enter a valid email address.</span>
            }
          </div>

          <div class="field-group">
            <label for="password" class="field-label">Password</label>
            <div class="input-wrapper" [class.error]="isFieldInvalid('password')">
              <mat-icon class="input-icon">lock_outline</mat-icon>
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                class="field-input"
                formControlName="password"
                placeholder="••••••••"
                autocomplete="current-password" />
              <button
                type="button"
                class="toggle-password"
                (click)="showPassword.update(v => !v)">
                <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
            </div>
            @if (isFieldInvalid('password')) {
              <span class="field-error">Password is required.</span>
            }
          </div>

          <button
            type="submit"
            class="submit-btn"
            [disabled]="isLoading()">
            @if (isLoading()) {
              <span class="btn-spinner"></span>
              Signing in...
            } @else {
              Sign In
              <mat-icon>arrow_forward</mat-icon>
            }
          </button>
        </form>

        <p class="register-link">
          New to ProVantage?
          <a routerLink="/register">Create an account</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .login-page {
      min-height: 100vh;
      background: $bg-primary;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      overflow: hidden;
    }

    .login-bg {
      position: absolute;
      inset: 0;
      pointer-events: none;
    }

    .bg-orb {
      position: absolute;
      border-radius: 50%;
      filter: blur(80px);
      opacity: 0.15;
    }

    .orb-1 {
      width: 600px; height: 600px;
      background: $color-primary;
      top: -200px; right: -100px;
    }

    .orb-2 {
      width: 400px; height: 400px;
      background: $color-accent;
      bottom: -100px; left: -100px;
    }

    .orb-3 {
      width: 300px; height: 300px;
      background: $color-warning;
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
    }

    .login-card {
      width: 100%;
      max-width: 420px;
      padding: $space-2xl;
      position: relative;
      z-index: 1;
    }

    .logo-block {
      display: flex;
      align-items: center;
      gap: $space-md;
      margin-bottom: $space-xl;
    }

    .logo-icon {
      width: 44px;
      height: 44px;
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 15%));
      border-radius: $radius-md;
      display: flex;
      align-items: center;
      justify-content: center;
      font-family: $font-heading;
      font-weight: 800;
      font-size: $text-xl;
      color: white;
    }

    .logo-text {
      font-family: $font-heading;
      font-weight: 700;
      font-size: $text-2xl;
      color: $text-primary;
    }

    .card-title {
      font-family: $font-heading;
      font-size: $text-3xl;
      font-weight: 700;
      color: $text-primary;
      margin-bottom: $space-xs;
    }

    .card-subtitle {
      font-size: $text-sm;
      color: $text-secondary;
      margin-bottom: $space-xl;
    }

    .error-banner {
      display: flex;
      align-items: center;
      gap: $space-sm;
      padding: $space-md;
      background: rgba($color-danger, 0.12);
      border: 1px solid rgba($color-danger, 0.3);
      border-radius: $radius-md;
      color: $color-danger;
      font-size: $text-sm;
      margin-bottom: $space-lg;

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .field-group {
      display: flex;
      flex-direction: column;
      gap: $space-xs;
      margin-bottom: $space-lg;
    }

    .field-label {
      font-size: $text-sm;
      font-weight: 500;
      color: $text-secondary;
    }

    .input-wrapper {
      display: flex;
      align-items: center;
      background: rgba(255, 255, 255, 0.04);
      border: 1px solid $border-subtle;
      border-radius: $radius-md;
      padding: 0 $space-md;
      transition: all $transition-fast;

      &:focus-within {
        border-color: $color-primary;
        box-shadow: 0 0 0 3px rgba($color-primary, 0.12);
      }

      &.error {
        border-color: $color-danger;
      }
    }

    .input-icon {
      color: $text-muted;
      font-size: 18px;
      width: 18px;
      height: 18px;
      flex-shrink: 0;
      margin-right: $space-sm;
    }

    .field-input {
      flex: 1;
      background: none;
      border: none;
      outline: none;
      color: $text-primary;
      font-size: $text-base;
      padding: 12px 0;
      font-family: $font-body;

      &::placeholder { color: $text-muted; }
    }

    .toggle-password {
      background: none;
      border: none;
      cursor: pointer;
      color: $text-muted;
      padding: 4px;
      display: flex;
      align-items: center;
      transition: color $transition-fast;

      &:hover { color: $text-secondary; }

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .field-error {
      font-size: $text-xs;
      color: $color-danger;
    }

    .submit-btn {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: $space-sm;
      padding: 14px $space-xl;
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 10%));
      color: white;
      border: none;
      border-radius: $radius-md;
      font-size: $text-base;
      font-weight: 600;
      font-family: $font-body;
      cursor: pointer;
      transition: all $transition-fast;
      margin-bottom: $space-lg;
      margin-top: $space-sm;

      &:hover:not(:disabled) {
        transform: translateY(-1px);
        box-shadow: $shadow-glow;
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .btn-spinner {
      width: 16px;
      height: 16px;
      border: 2px solid rgba(255, 255, 255, 0.3);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .register-link {
      text-align: center;
      font-size: $text-sm;
      color: $text-secondary;

      a {
        color: $color-primary;
        text-decoration: none;
        font-weight: 600;
        &:hover { text-decoration: underline; }
      }
    }
  `]
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = signal(false);
  showPassword = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  isFieldInvalid(field: string): boolean {
    const control = this.loginForm.get(field);
    return !!(control && control.invalid && control.touched);
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.login(this.loginForm.value).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.errorMessage.set(
          err.error?.detail ?? err.error?.message ?? 'Invalid email or password.'
        );
        this.isLoading.set(false);
      }
    });
  }
}
