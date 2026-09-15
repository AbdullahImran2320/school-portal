export type AdmissionStatus = 'Applied' | 'Admitted' | 'Withdrawn' | 'Rejected' | 'Graduated';

export interface StudentDto {
  studentId: number;
  name: string;
  rollNumber: number | null;
  bFormNumber: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  admissionStatus: AdmissionStatus;
  classId: number;
  className: string;
  section: string;
  parentId: number;
  fatherName: string;
  fatherMobile: string;
  motherName?: string;
  motherMobile?: string;
}

export interface CreateStudentDto {
  name: string;
  // Leave undefined to auto-assign the next available number in admission
  // order; set explicitly to skip auto-assignment.
  rollNumber?: number;
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