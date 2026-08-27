import { Component } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [NgIcon],
  template: `
    <button class="theme-toggle" (click)="themeService.toggle()" [attr.aria-label]="'Switch to ' + (themeService.theme() === 'light' ? 'dark' : 'light') + ' mode'">
      <ng-icon [name]="themeService.theme() === 'light' ? 'tablerMoon' : 'tablerSun'" />
    </button>
  `,
  styles: [`
    .theme-toggle {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 36px;
      height: 36px;
      border-radius: var(--radius);
      border: 1px solid var(--border-strong);
      background: transparent;
      color: var(--text-secondary);
      cursor: pointer;

      &:hover {
        background: var(--surface-accent);
        color: var(--text-primary);
      }
    }
  `]
})
export class ThemeToggleComponent {
  constructor(public themeService: ThemeService) {}
}