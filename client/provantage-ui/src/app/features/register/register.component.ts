import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, MatIconModule],
  template: `
    <div class="register-page">
      <div class="login-bg">
        <div class="bg-orb orb-1"></div>
        <div class="bg-orb orb-2"></div>
        <div class="bg-orb orb-3"></div>
      </div>

      <div class="register-card glass-card animate-fade-in-up">
        <!-- Logo -->
        <div class="logo-block">
          <div class="logo-icon">P</div>
          <span class="logo-text">ProVantage</span>
        </div>

        <h1 class="card-title">Create your workspace</h1>
        <p class="card-subtitle">Set up your company's procurement platform</p>

        @if (errorMessage()) {
          <div class="error-banner">
            <mat-icon>error_outline</mat-icon>
            {{ errorMessage() }}
          </div>
        }

        <form [formGroup]="registerForm" (ngSubmit)="onSubmit()" novalidate>

          <!-- Name row -->
          <div class="field-row">
            <div class="field-group">
              <label class="field-label">First Name</label>
              <div class="input-wrapper" [class.error]="isInvalid('firstName')">
                <mat-icon class="input-icon">person_outline</mat-icon>
                <input type="text" class="field-input" formControlName="firstName" placeholder="John" />
              </div>
              @if (isInvalid('firstName')) {
                <span class="field-error">Required.</span>
              }
            </div>
            <div class="field-group">
              <label class="field-label">Last Name</label>
              <div class="input-wrapper" [class.error]="isInvalid('lastName')">
                <mat-icon class="input-icon">person_outline</mat-icon>
                <input type="text" class="field-input" formControlName="lastName" placeholder="Doe" />
              </div>
              @if (isInvalid('lastName')) {
                <span class="field-error">Required.</span>
              }
            </div>
          </div>

          <!-- Email -->
          <div class="field-group">
            <label class="field-label">Work Email</label>
            <div class="input-wrapper" [class.error]="isInvalid('email')">
              <mat-icon class="input-icon">mail_outline</mat-icon>
              <input type="email" class="field-input" formControlName="email" placeholder="you@company.com" autocomplete="email" />
            </div>
            @if (isInvalid('email')) {
              <span class="field-error">Please enter a valid email address.</span>
            }
          </div>

          <!-- Company row -->
          <div class="field-row">
            <div class="field-group">
              <label class="field-label">Company Name</label>
              <div class="input-wrapper" [class.error]="isInvalid('tenantName')">
                <mat-icon class="input-icon">business</mat-icon>
                <input type="text" class="field-input" formControlName="tenantName" placeholder="Acme Corp" />
              </div>
              @if (isInvalid('tenantName')) {
                <span class="field-error">Required.</span>
              }
            </div>
            <div class="field-group">
              <label class="field-label">Subdomain</label>
              <div class="input-wrapper" [class.error]="isInvalid('tenantSubdomain')">
                <mat-icon class="input-icon">link</mat-icon>
                <input type="text" class="field-input" formControlName="tenantSubdomain" placeholder="acme-corp"
                  (input)="autoSlug($event)" />
              </div>
              @if (isInvalid('tenantSubdomain')) {
                <span class="field-error">Lowercase letters, numbers and hyphens only.</span>
              }
            </div>
          </div>

          <!-- Password -->
          <div class="field-group">
            <label class="field-label">Password</label>
            <div class="input-wrapper" [class.error]="isInvalid('password')">
              <mat-icon class="input-icon">lock_outline</mat-icon>
              <input
                [type]="showPassword() ? 'text' : 'password'"
                class="field-input"
                formControlName="password"
                placeholder="Min 8 chars, 1 uppercase, 1 digit"
                autocomplete="new-password" />
              <button type="button" class="toggle-password" (click)="togglePassword()">
                <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
            </div>
            @if (isInvalid('password')) {
              <span class="field-error">Min 8 characters, 1 uppercase letter and 1 digit required.</span>
            }
          </div>

          <button type="submit" class="submit-btn" [disabled]="isLoading()">
            @if (isLoading()) {
              <span class="btn-spinner"></span>
              Creating workspace...
            } @else {
              Create Account
              <mat-icon>arrow_forward</mat-icon>
            }
          </button>
        </form>

        <p class="login-link">
          Already have an account?
          <a routerLink="/login">Sign in</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .register-page {
      min-height: 100vh;
      background: $bg-primary;
      display: flex;
      align-items: center;
      justify-content: center;
      position: relative;
      overflow: hidden;
      padding: $space-xl 0;
    }

    .login-bg {
      position: absolute; inset: 0; pointer-events: none;
    }

    .bg-orb {
      position: absolute; border-radius: 50%; filter: blur(80px); opacity: 0.15;
    }
    .orb-1 { width: 600px; height: 600px; background: $color-primary; top: -200px; right: -100px; }
    .orb-2 { width: 400px; height: 400px; background: $color-accent; bottom: -100px; left: -100px; }
    .orb-3 { width: 300px; height: 300px; background: $color-warning; top: 50%; left: 50%; transform: translate(-50%, -50%); }

    .register-card {
      width: 100%; max-width: 540px;
      padding: $space-2xl;
      position: relative; z-index: 1;
    }

    .logo-block {
      display: flex; align-items: center; gap: $space-md; margin-bottom: $space-xl;
    }

    .logo-icon {
      width: 44px; height: 44px;
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 15%));
      border-radius: $radius-md;
      display: flex; align-items: center; justify-content: center;
      font-family: $font-heading; font-weight: 800; font-size: $text-xl; color: white;
    }

    .logo-text { font-family: $font-heading; font-weight: 700; font-size: $text-2xl; color: $text-primary; }

    .card-title { font-family: $font-heading; font-size: $text-3xl; font-weight: 700; color: $text-primary; margin-bottom: $space-xs; }
    .card-subtitle { font-size: $text-sm; color: $text-secondary; margin-bottom: $space-xl; }

    .error-banner {
      display: flex; align-items: center; gap: $space-sm;
      padding: $space-md;
      background: rgba($color-danger, 0.12); border: 1px solid rgba($color-danger, 0.3);
      border-radius: $radius-md; color: $color-danger; font-size: $text-sm; margin-bottom: $space-lg;
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .field-row {
      display: grid; grid-template-columns: 1fr 1fr; gap: $space-md;
    }

    .field-group {
      display: flex; flex-direction: column; gap: $space-xs; margin-bottom: $space-lg;
    }

    .field-label { font-size: $text-sm; font-weight: 500; color: $text-secondary; }

    .input-wrapper {
      display: flex; align-items: center;
      background: rgba(255,255,255,0.04); border: 1px solid $border-subtle;
      border-radius: $radius-md; padding: 0 $space-md; transition: all $transition-fast;
      &:focus-within { border-color: $color-primary; box-shadow: 0 0 0 3px rgba($color-primary, 0.12); }
      &.error { border-color: $color-danger; }
    }

    .input-icon { color: $text-muted; font-size: 18px; width: 18px; height: 18px; flex-shrink: 0; margin-right: $space-sm; }

    .field-input {
      flex: 1; background: none; border: none; outline: none;
      color: $text-primary; font-size: $text-base; padding: 12px 0; font-family: $font-body;
      &::placeholder { color: $text-muted; }
    }

    .toggle-password {
      background: none; border: none; cursor: pointer; color: $text-muted; padding: 4px;
      display: flex; align-items: center; transition: color $transition-fast;
      &:hover { color: $text-secondary; }
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .field-error { font-size: $text-xs; color: $color-danger; }

    .submit-btn {
      width: 100%; display: flex; align-items: center; justify-content: center; gap: $space-sm;
      padding: 14px $space-xl;
      background: linear-gradient(135deg, $color-primary, lighten($color-primary, 10%));
      color: white; border: none; border-radius: $radius-md;
      font-size: $text-base; font-weight: 600; font-family: $font-body;
      cursor: pointer; transition: all $transition-fast;
      margin-bottom: $space-lg; margin-top: $space-sm;
      &:hover:not(:disabled) { transform: translateY(-1px); box-shadow: $shadow-glow; }
      &:disabled { opacity: 0.6; cursor: not-allowed; }
      mat-icon { font-size: 18px; width: 18px; height: 18px; }
    }

    .btn-spinner {
      width: 16px; height: 16px;
      border: 2px solid rgba(255,255,255,0.3); border-top-color: white;
      border-radius: 50%; animation: spin 0.7s linear infinite;
    }

    @keyframes spin { to { transform: rotate(360deg); } }

    .login-link {
      text-align: center; font-size: $text-sm; color: $text-secondary;
      a { color: $color-primary; text-decoration: none; font-weight: 600; &:hover { text-decoration: underline; } }
    }
  `]
})
export class RegisterComponent {
  registerForm: FormGroup;
  isLoading = signal(false);
  showPassword = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private authService: AuthService,
    private router: Router
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName:  ['', [Validators.required, Validators.maxLength(100)]],
      email:     ['', [Validators.required, Validators.email]],
      tenantName:      ['', [Validators.required, Validators.maxLength(200)]],
      tenantSubdomain: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
      password:  ['', [Validators.required, Validators.minLength(8),
                       Validators.pattern(/(?=.*[A-Z])(?=.*[0-9])/)]],
    });
  }

  togglePassword(): void {
    this.showPassword.update(v => !v);
  }

  isInvalid(field: string): boolean {
    const c = this.registerForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  autoSlug(event: Event): void {
    const val = (event.target as HTMLInputElement).value
      .toLowerCase().replace(/[^a-z0-9-]/g, '-').replace(/-+/g, '-');
    this.registerForm.get('tenantSubdomain')!.setValue(val, { emitEvent: false });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.http.post<any>('/api/auth/register', this.registerForm.value).subscribe({
      next: (res) => {
        this.authService.handleAuthResponse(res);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        const detail = err.error?.errors
          ? Object.values(err.error.errors).flat().join(' ')
          : (err.error?.error ?? err.error?.message ?? 'Registration failed. Please try again.');
        this.errorMessage.set(detail as string);
        this.isLoading.set(false);
      }
    });
  }
}
