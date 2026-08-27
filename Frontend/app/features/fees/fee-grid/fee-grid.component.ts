import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClassesService } from '../../students/services/classes.service';
import { FeesService } from '../services/fees.service';
import { CommonModule, DecimalPipe } from '@angular/common';
import { ClassDto } from '../../students/models/class.models';
import { ClassFeeGrid, MonthCell, StudentFeeRow } from '../models/fee.models';
import { PaymentDialogComponent, PaymentTarget } from '../payment-dialog/payment-dialog.component';
import { FeesNavComponent } from '../../../shared/components/fees-nav/fees-nav.component';
import { AuthService } from '../../../core/services/auth.service';

const MONTH_LABELS = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

@Component({
  selector: 'app-fee-grid',
  standalone: true,
  imports: [FormsModule, PaymentDialogComponent,DecimalPipe, FeesNavComponent, CommonModule],
  templateUrl: './fee-grid.component.html',
  styleUrl: './fee-grid.component.scss'
})
export class FeeGridComponent implements OnInit {
  private classesService = inject(ClassesService);
  private feesService = inject(FeesService);
  auth = inject(AuthService);

  monthLabels = MONTH_LABELS;
  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedYear = signal<number>(new Date().getFullYear());
  grid = signal<ClassFeeGrid | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  paymentTarget = signal<PaymentTarget | null>(null);
  concessionStudent = signal<StudentFeeRow | null>(null);
  concessionAmount = signal<number>(0);
  concessionReason = signal('');
  concessionSaving = signal(false);
  concessionError = signal<string | null>(null);

  yearOptions = computed(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1];
  });

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => {
        this.classes.set(data);
        if (data.length > 0) {
          this.selectedClassId.set(data[0].classId);
          this.loadGrid();
        }
      },
      error: () => this.error.set('Could not load classes.')
    });
  }

  onClassChange() { this.loadGrid(); }
  onYearChange() { this.loadGrid(); }

  loadGrid() {
    const classId = this.selectedClassId();
    if (!classId) return;

    this.loading.set(true);
    this.error.set(null);
    this.feesService.getClassFeeGrid(classId, this.selectedYear()).subscribe({
      next: (data) => {
        this.grid.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load the fee grid.');
        this.loading.set(false);
      }
    });
  }

  openCell(student: StudentFeeRow, month: MonthCell) {
    this.paymentTarget.set({
      kind: 'ledger',
      id: month.ledgerId,
      studentName: student.studentName,
      label: `${this.monthLabels[month.monthNumber - 1]} ${this.selectedYear()}`,
      dueAmount: month.dueAmount,
      discountAmount: month.discountAmount,
      paidAmount: month.paidAmount,
      lateFeeAmount: month.lateFeeAmount,
      status: month.status
    });
  }

  onPaymentRecorded() {
    this.paymentTarget.set(null);
    this.loadGrid();
  }

  closeDialog() {
    this.paymentTarget.set(null);
  }

  isAdmin() {
    return this.auth.role() === 'Admin';
  }

  getMonthCell(student: StudentFeeRow, monthNumber: number): MonthCell | undefined {
    return student.months.find((month) => month.monthNumber === monthNumber);
  }

  monthOutstanding(month: MonthCell): number {
    return Math.max(month.dueAmount - month.discountAmount + month.lateFeeAmount - month.paidAmount, 0);
  }

  openConcession(student: StudentFeeRow) {
    const firstApplicable = student.months[0];
    const current = firstApplicable?.discountAmount ?? 0;
    this.concessionStudent.set(student);
    this.concessionAmount.set(current);
    this.concessionReason.set('');
    this.concessionError.set(null);
  }

  closeConcession() {
    if (this.concessionSaving()) return;
    this.concessionStudent.set(null);
    this.concessionError.set(null);
  }

  saveConcession() {
    const student = this.concessionStudent();
    const amount = Number(this.concessionAmount());
    if (!student) return;
    if (!Number.isFinite(amount) || amount < 0) {
      this.concessionError.set('Enter a valid concession amount.');
      return;
    }
    this.concessionSaving.set(true);
    this.concessionError.set(null);
    this.feesService.setStudentDiscount(student.studentId, {
      monthlyDiscountAmount: amount,
      reason: this.concessionReason().trim() || undefined,
      applyToRemainingMonthsThisYear: true
    }).subscribe({
      next: () => {
        this.concessionSaving.set(false);
        this.closeConcession();
        this.loadGrid();
      },
      error: (err) => {
        this.concessionSaving.set(false);
        this.concessionError.set(err?.error?.message ?? 'Could not save the student concession.');
      }
    });
  }

}