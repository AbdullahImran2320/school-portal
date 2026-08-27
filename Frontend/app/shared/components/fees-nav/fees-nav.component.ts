
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-fees-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="fees-nav">
      <a routerLink="/fees" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">Fee Grid</a>
      <a routerLink="/fees/defaulters" routerLinkActive="active">Defaulters</a>
      <a routerLink="/fees/components" routerLinkActive="active">Fee Structure</a>
      <a routerLink="/fees/vouchers" routerLinkActive="active">Vouchers</a>
    </nav>
  `,
  styles: [`
    .fees-nav { display: flex; gap: 4px; margin-bottom: 20px; border-bottom: 1px solid var(--border); }
    a { padding: 8px 14px; font-size: 13px; color: var(--text-secondary); text-decoration: none; border-bottom: 2px solid transparent; margin-bottom: -1px; }
    a:hover { color: var(--text-primary); }
    a.active { color: var(--accent-text); border-bottom-color: var(--accent); font-weight: 500; }
  `]
})
export class FeesNavComponent {
    
}