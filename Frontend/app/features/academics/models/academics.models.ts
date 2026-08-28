export interface SubjectDto {
  subjectId: number;
  subjectName: string;
  classId: number;
  className: string;
}

export interface CreateSubjectDto {
  subjectName: string;
  classId: number;
}

export interface ExamDto {
  examId: number;
  examName: string;
  term: string;
  academicYear: string;
}

export interface CreateExamDto {
  examName: string;
  term: string;
  academicYear: string;
}

export interface RecordResultDto {
  studentId: number;
  subjectId: number;
  examId: number;
  marksObtained: number;
  totalMarks: number;
}

export interface ExistingResultDto {
  studentId: number;
  marksObtained: number;
  totalMarks: number;
}

export interface ResultDto {
  resultId: number;
  subjectName: string;
  marksObtained: number;
  totalMarks: number;
  percentage: number;
  passFail: string;
}

export interface ReportCardDto {
  studentId: number;
  studentName: string;
  examName: string;
  term: string;
  subjects: ResultDto[];
  overallPercentage: number;
  overallResult: string;
}