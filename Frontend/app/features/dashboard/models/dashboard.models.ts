export interface DashboardSummary {
  totalActiveStudents: number;
  feeChallansGeneratedThisMonth: number;
  feeAmountCollectedThisMonth: number;
  attendanceDate: string;
  attendance: ClassAttendanceSummary[];
}

export interface ClassAttendanceSummary {
  classId: number;
  className: string;
  totalStudents: number;
  present: number;
  absent: number;
  unmarked: number;
  otherMarked: number;
}
