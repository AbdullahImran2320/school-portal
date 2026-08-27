export type LedgerStatus = 'Unpaid' | 'Partial' | 'Paid' | 'Overdue';
export type ChargeStatus = 'Unpaid' | 'Partial' | 'Paid';

export interface MonthCell {
  ledgerId: number;
  monthNumber: number;
  dueAmount: number;
  discountAmount: number;
  paidAmount: number;
  lateFeeAmount: number;
  status: LedgerStatus;
}

export interface StudentFeeRow {
  studentId: number;
  studentName: string;
  months: MonthCell[];
  totalOutstanding: number;
}

export interface ClassFeeGrid {
  classId: number;
  className: string;
  students: StudentFeeRow[];
}

export interface OneOffCharge {
  chargeId: number;
  chargeType: string;
  dueAmount: number;
  discountAmount: number;
  paidAmount: number;
  status: ChargeStatus;
}

export interface FeeSummary {
  monthlyLedger: MonthCell[];
  oneOffCharges: OneOffCharge[];
}

export interface Defaulter {
  studentId: number;
  studentName: string;
  className: string;
  fatherMobile: string;
  overdueMonthsCount: number;
  totalOutstanding: number;
}

export interface RecordPaymentRequest {
  amountPaid: number;
  paymentMethod: string;
  collectedBy: string;
  discountAmount: number;
}

export interface SetStudentDiscountRequest {
  monthlyDiscountAmount: number;
  reason?: string;
  applyToRemainingMonthsThisYear: boolean;
}

export interface PaymentResult {
  paymentId: number;
  receiptNumber: string;
  amountPaid: number;
  newPaidTotal: number;
  dueAmount: number;
  lateFeeCharged: number;
  status: LedgerStatus;
}

export type FeeFrequency = 'OneTime' | 'Yearly' | 'Monthly';

export interface FeeComponent {
  feeComponentId: number;
  componentName: string;
  amount: number;
  frequency: FeeFrequency;
  academicYear: string;
  classId: number;
  className: string;
}

export interface UpsertFeeComponent {
  componentName: string;
  amount: number;
  frequency: FeeFrequency;
  academicYear: string;
  classId: number;
}

export interface VoucherCharge {
  chargeType: string;
  balance: number;
}

export interface FeeVoucher {
  schoolName: string;
  campusName: string;
  challanNumber: string;
  issueDate: string;
  dueDate: string;
  studentId: number;
  studentName: string;
  bFormNumber: string;
  className: string;
  fatherName: string;
  fatherMobile: string;
  voucherMonth: number;
  voucherYear: number;
  monthlyFeeDue: number;
  discountAmount: number;
  lateFeeAmount: number;
  monthlyNetPayable: number;
  outstandingCharges: VoucherCharge[];
  totalAmountDue: number;
}