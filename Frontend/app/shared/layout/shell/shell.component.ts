import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { TopbarComponent } from '../topbar/topbar.component';
import { LicenseWarningComponent } from '../../../features/license/components/license-warning/license-warning.component';
import { LicenseService } from '../../../features/license/services/license.service';
import { LicenseStatus } from '../../../features/license/models/license.models';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, LicenseWarningComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  licenseStatus: LicenseStatus | null = null;

  constructor(private licenseService: LicenseService) {
    this.licenseService.getStatus().subscribe({
      next: status => this.licenseStatus = status,
      error: () => this.licenseStatus = null
    });
  }
}