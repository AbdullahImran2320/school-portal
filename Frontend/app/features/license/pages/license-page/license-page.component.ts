import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { LicenseService } from '../../services/license.service';
import { LicenseStatus } from '../../models/license.models';

@Component({
  selector: 'app-license-page',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './license-page.component.html',
  styleUrl: './license-page.component.scss'
})
export class LicensePageComponent {
  private readonly licenseService = inject(LicenseService);

  status: LicenseStatus | null = null;
  licenseKey = '';
  loading = true;
  activating = false;
  message = '';
  error = '';

  constructor() {
    this.loadStatus();
  }

  loadStatus() {
    this.loading = true;
    this.licenseService.getStatus().subscribe({
      next: status => {
        this.status = status;
        this.loading = false;
      },
      error: () => {
        this.error = 'Unable to read the current license status.';
        this.loading = false;
      }
    });
  }

  activate() {
    this.message = '';
    this.error = '';
    this.activating = true;

    this.licenseService.activate(this.licenseKey).subscribe({
      next: result => {
        this.activating = false;
        if (result.success) {
          this.message = result.message;
          this.licenseKey = '';
          this.loadStatus();
        } else {
          this.error = result.message;
        }
      },
      error: err => {
        this.activating = false;
        this.error = err?.error?.message ?? 'License activation failed.';
      }
    });
  }
}
