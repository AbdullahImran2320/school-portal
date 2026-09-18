export type AdmissionStatus = 'Applied' | 'Admitted' | 'Withdrawn' | 'Rejected' | 'Graduated';

export interface StudentDto {
  studentId: number;
  name: string;
  rollNumber: string | null;
  // Position within this student's own class/section — what an editable
  // roll number input reads and writes; rollNumber (the formatted code
  // above) is always regenerated from it, never typed directly.
  rollNumberSequence: number | null;
  bFormNumber: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  admissionStatus: AdmissionStatus;
  classId: number;
  className: string;
  section: string;
  hasPhoto: boolean;
  parentId: number;
  fatherName: string;
  fatherMobile: string;
  motherName?: string;
  motherMobile?: string;
}

export interface CreateStudentDto {
  name: string;
  // Leave undefined to auto-assign the next available position in this
  // student's class/section; set explicitly to skip auto-assignment.
  rollNumberSequence?: number;
  bFormNumber: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  admissionStatus: AdmissionStatus;
  classId: number;
  parentId: number;
}

export interface UpdateStudentDto {
  name: string;
  bFormNumber: string;
  dateOfBirth: string;
  gender: string;
  admissionStatus: AdmissionStatus;
  classId: number;
}