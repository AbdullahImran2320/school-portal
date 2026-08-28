import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FeesService } from '../services/fees.service';
import { FeeSummary, OneOffCharge } from '../models/fee.models';
import { PaymentDialogComponent, PaymentTarget } from '../payment-dialog/payment-dialog.component';

@Component({
  selector: 'app-student-charges',
  standalone: true,
  imports: [CommonModule, DecimalPipe, PaymentDialogComponent, RouterLink],
  templateUrl: './student-charges.component.html',
  styleUrl: './student-charges.component.scss'
})
export class StudentChargesComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private feesService = inject(FeesService);

  studentId = signal<number | null>(null);
  summary = signal<FeeSummary | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  paymentTarget = signal<PaymentTarget | null>(null);

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('studentId'));
    if (!id) {
      this.error.set('No student specified.');
      return;
    }
    this.studentId.set(id);
    this.load();
  }

  load() {
    const id = this.studentId();
    if (!id) return;

    this.loading.set(true);
    this.error.set(null);
    this.feesService.getStudentFeeSummary(id).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set("Could not load this student's charges.");
        this.loading.set(false);
      }
    });
  }

  outstanding(charge: OneOffCharge): number {
    return Math.max(charge.dueAmount - charge.discountAmount - charge.paidAmount, 0);
  }

  payCharge(charge: OneOffCharge) {
    const summary = this.summary();
    if (!summary) return;

    this.paymentTarget.set({
      kind: 'charge',
      id: charge.chargeId,
      studentName: summary.studentName,
      label: charge.chargeType,
      dueAmount: charge.dueAmount,
      discountAmount: charge.discountAmount,
      paidAmount: charge.paidAmount,
      lateFeeAmount: 0,
      status: charge.status
    });
  }

  onPaymentRecorded() {
    this.paymentTarget.set(null);
    this.load();
  }

  closeDialog() {
    this.paymentTarget.set(null);
  }
}
