export interface RecentPayment {
  paymentDate: string;
  amountPaid: number;
  paidAgainst: string;
  receiptNumber: string;
}

export interface StudentProfile {
  schoolName: string;
  campusName: string;
  studentId: number;
  name: string;
  rollNumber: number | null;
  bFormNumber: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  admissionStatus: string;
  classId: number;
  className: string;
  section: string;
  hasPhoto: boolean;
  fatherName: string;
  fatherMobile: string;
  motherName: string | null;
  motherMobile: string | null;
  monthlyDiscountAmount: number;
  discountReason: string | null;

  totalOutstanding: number;
  overdueMonthsCount: number;
  recentPayments: RecentPayment[];

  attendanceMonth: number;
  attendanceYear: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  lateDays: number;
  attendancePercentage: number;

  latestExamName: string | null;
  latestExamTerm: string | null;
  latestExamPercentage: number | null;
  latestExamResult: string | null;
}
