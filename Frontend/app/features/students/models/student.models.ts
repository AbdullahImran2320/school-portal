export type AdmissionStatus = 'Applied' | 'Admitted' | 'Withdrawn' | 'Rejected' | 'Graduated';

export interface StudentDto {
  studentId: number;
  name: string;
  bFormNumber: string;
  dateOfBirth: string;
  gender: string;
  admissionDate: string;
  admissionStatus: AdmissionStatus;
  classId: number;
  className: string;
  parentId: number;
  fatherName: string;
  fatherMobile: string;
  motherName?: string;
  motherMobile?: string;
}

export interface CreateStudentDto {
  name: string;
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