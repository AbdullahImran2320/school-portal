import { Component, Input, Output, EventEmitter, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { FeesService } from '../services/fees.service';
import { CommonModule, DecimalPipe } from '@angular/common';

export interface PaymentTarget {
  kind: 'ledger' | 'charge';
  id: number;
  studentName: string;
  label: string;
  dueAmount: number;
  discountAmount: number;
  paidAmount: number;
  lateFeeAmount: number;
  status: string;
}

@Component({
  selector: 'app-payment-dialog',
  standalone: true,
  imports: [FormsModule, DecimalPipe],
  templateUrl: './payment-dialog.component.html',
  styleUrl: './payment-dialog.component.scss'
})
export class PaymentDialogComponent {
  @Input({ required: true }) target!: PaymentTarget;
  @Output() recorded = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private feesService = inject(FeesService);

  amount = signal<number | null>(null);
  discount = signal<number | null>(0);
  fine = signal<number | null>(null);
  method = signal('Cash');
  collectedBy = signal('');
  submitting = signal(false);
  error = signal<string | null>(null);

  outstanding(): number {
    return Math.max(
      (this.target.dueAmount - this.target.discountAmount + this.target.lateFeeAmount) - this.target.paidAmount,
      0
    );
  }

  autoFine(): number {
    return this.target.lateFeeAmount ?? 0;
  }

  effectiveFine(): number {
    const value = this.fine();
    return value === null ? this.autoFine() : Math.max(Number(value) || 0, 0);
  }

  baseOutstandingAfterDiscount(): number {
    return Math.max(this.outstanding() - (this.discount() ?? 0) - this.autoFine(), 0);
  }

  outstandingAfterDiscount(): number {
    return Math.max(this.outstanding() - (this.discount() ?? 0), 0);
  }

  totalPayable(): number {
    const beforeFine = Math.max(this.outstanding() - (this.discount() ?? 0), 0);
    return Math.max(beforeFine + this.effectiveFine() - this.autoFine(), 0);
  }

  setFine(value: number | null) {
    this.fine.set(value);
    this.amount.set(this.totalPayable());
  }

  fillFullAmount() {
    this.amount.set(this.totalPayable());
  }

  submit() {
    const amt = this.amount();
    if (!amt || amt <= 0) {
      this.error.set('Enter an amount greater than 0.');
      return;
    }
    if (!this.collectedBy().trim()) {
      this.error.set('Enter who collected this payment.');
      return;
    }

    const discount = Math.max(this.discount() ?? 0, 0);
    const selectedFine = this.fine();
    const fine = selectedFine === null ? null : Math.max(Number(selectedFine) || 0, 0);
    const maxPayable = this.outstanding() - discount - this.autoFine() + (fine ?? this.autoFine());
    if (discount > this.outstanding()) {
      this.error.set('Discount cannot be greater than the current outstanding amount.');
      return;
    }
    if (amt > Math.max(maxPayable, 0)) {
      this.error.set('Payment cannot be greater than the final payable amount.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    const dto = {
      amountPaid: amt,
      paymentMethod: this.method(),
      collectedBy: this.collectedBy(),
      discountAmount: discount,
      fineAmount: fine
    };
    const request$ = this.target.kind === 'ledger'
      ? this.feesService.payLedger(this.target.id, dto)
      : this.feesService.payCharge(this.target.id, dto);

    request$.subscribe({
      next: () => this.recorded.emit(),
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error?.message ?? 'Could not record the payment. Please try again.');
      }
    });
  }

  close() {
    this.cancelled.emit();
  }
}