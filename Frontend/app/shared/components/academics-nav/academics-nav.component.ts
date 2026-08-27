import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-academics-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="academics-nav">
      <a routerLink="/academics/subjects" routerLinkActive="active">Subjects</a>
      <a routerLink="/academics/exams" routerLinkActive="active">Exams</a>
      <a routerLink="/academics/results" routerLinkActive="active">Record Results</a>
      <a routerLink="/academics/report-card" routerLinkActive="active">Report Card</a>
    </nav>
  `,
  styles: [`
    .academics-nav { display: flex; gap: 4px; margin-bottom: 20px; border-bottom: 1px solid var(--border); }
    a { padding: 8px 14px; font-size: 13px; color: var(--text-secondary); text-decoration: none; border-bottom: 2px solid transparent; margin-bottom: -1px; }
    a:hover { color: var(--text-primary); }
    a.active { color: var(--accent-text); border-bottom-color: var(--accent); font-weight: 500; }
  `]
})
export class AcademicsNavComponent {}
