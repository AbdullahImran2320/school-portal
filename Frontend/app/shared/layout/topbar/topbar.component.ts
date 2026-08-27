import { Component } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeToggleComponent } from '../../components/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [NgIcon, ThemeToggleComponent],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss'
})
export class TopbarComponent {
  constructor(public auth: AuthService, public themeService: ThemeService) {}

  logout() {
    this.auth.logout();
  }
}