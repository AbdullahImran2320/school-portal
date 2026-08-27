export type AttendanceStatus = 'Present' | 'Absent' | 'Leave' | 'Late' | 'NotMarked';

export interface MarkAttendanceEntry {
  studentId: number;
  status: string;
}

export interface BulkMarkAttendance {
  classId: number;
  date: string;
  entries: MarkAttendanceEntry[];
}

export interface ClassAttendanceRow {
  studentId: number;
  studentName: string;
  status: AttendanceStatus;
}

export interface StudentAttendanceSummary {
  studentId: number;
  studentName: string;
  totalMarkedDays: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  lateDays: number;
  attendancePercentage: number;
}
