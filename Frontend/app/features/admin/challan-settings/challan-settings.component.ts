import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { VouchersService } from '../../fees/services/vouchers.service';
import { ChallanSettings } from '../../fees/models/fee.models';

@Component({
  selector: 'app-challan-settings',
  standalone: true,
  imports: [FormsModule, AdminNavComponent],
  templateUrl: './challan-settings.component.html',
  styleUrl: './challan-settings.component.scss'
})
export class ChallanSettingsComponent implements OnInit {
  private vouchersService = inject(VouchersService);

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  saved = signal(false);

  accountTitle = signal('');
  bankName = signal('');
  accountNumber = signal('');
  paymentTermsLine1 = signal('');
  paymentTermsLine2 = signal('');

  ngOnInit() {
    this.loading.set(true);
    this.vouchersService.getChallanSettings().subscribe({
      next: (s) => {
        this.accountTitle.set(s.accountTitle);
        this.bankName.set(s.bankName);
        this.accountNumber.set(s.accountNumber);
        this.paymentTermsLine1.set(s.paymentTermsLine1);
        this.paymentTermsLine2.set(s.paymentTermsLine2);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load challan settings.');
        this.loading.set(false);
      }
    });
  }

  save() {
    const dto: ChallanSettings = {
      accountTitle: this.accountTitle().trim(),
      bankName: this.bankName().trim(),
      accountNumber: this.accountNumber().trim(),
      paymentTermsLine1: this.paymentTermsLine1().trim(),
      paymentTermsLine2: this.paymentTermsLine2().trim()
    };

    this.error.set(null);
    this.saved.set(false);
    this.saving.set(true);
    this.vouchersService.updateChallanSettings(dto).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); },
      error: (err) => {
        this.error.set(err?.error ?? 'Could not save challan settings.');
        this.saving.set(false);
      }
    });
  }
}
