import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-attendance-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="attendance-nav">
      <a routerLink="/attendance" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">Mark Attendance</a>
      <a routerLink="/attendance/report" routerLinkActive="active">Student Report</a>
    </nav>
  `,
  styles: [`
    .attendance-nav { display: flex; gap: 4px; margin-bottom: 20px; border-bottom: 1px solid var(--border); }
    a { padding: 8px 14px; font-size: 13px; color: var(--text-secondary); text-decoration: none; border-bottom: 2px solid transparent; margin-bottom: -1px; }
    a:hover { color: var(--text-primary); }
    a.active { color: var(--accent-text); border-bottom-color: var(--accent); font-weight: 500; }
  `]
})
export class AttendanceNavComponent {}
