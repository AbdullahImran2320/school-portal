import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LicenseService } from '../../services/license.service';

@Component({
  selector: 'app-license-expired',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './license-expired.component.html',
  styleUrl: './license-expired.component.scss'
})
export class LicenseExpiredComponent {
  private readonly licenseService = inject(LicenseService);
  private readonly router = inject(Router);

  licenseKey = '';
  error = '';
  activating = false;

  activate() {
    this.error = '';
    this.activating = true;

    this.licenseService.activate(this.licenseKey).subscribe({
      next: result => {
        this.activating = false;
        if (result.success) {
          this.router.navigate(['/dashboard']);
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
