import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent],
  template: `
    <div class="app-shell">
      <app-sidebar />
      <div class="main-area">
        <app-header />
        <main class="main-content">
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  styles: [`
    @use 'styles/variables' as *;

    .app-shell {
      display: flex;
      height: 100vh;
      overflow: hidden;
    }

    .main-area {
      flex: 1;
      margin-left: $sidebar-width;
      display: flex;
      flex-direction: column;
      transition: margin-left $transition-smooth;
      min-width: 0;
    }

    .main-content {
      flex: 1;
      overflow-y: auto;
      background: $bg-primary;
    }
  `]
})
export class ShellComponent {}
