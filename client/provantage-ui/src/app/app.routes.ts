import { Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Public routes
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/login/login.component').then(m => m.LoginComponent)
  },

  // Protected routes inside shell
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component')
            .then(m => m.DashboardComponent)
      },
      {
        path: 'vendors',
        loadComponent: () =>
          import('./features/vendors/vendors.component')
            .then(m => m.VendorsComponent)
      },
      {
        path: 'requisitions',
        loadComponent: () =>
          import('./features/requisitions/requisitions.component')
            .then(m => m.RequisitionsComponent)
      },
      {
        path: 'purchase-orders',
        loadComponent: () =>
          import('./features/purchase-orders/purchase-orders.component')
            .then(m => m.PurchaseOrdersComponent)
      },
      {
        path: 'invoices',
        loadComponent: () =>
          import('./features/invoices/invoices.component')
            .then(m => m.InvoicesComponent)
      },
      {
        path: 'budgets',
        loadComponent: () =>
          import('./features/budgets/budgets.component')
            .then(m => m.BudgetsComponent)
      },
    ]
  },

  { path: '**', redirectTo: 'dashboard' }
];
