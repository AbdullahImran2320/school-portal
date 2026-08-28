import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { 
  SubjectDto, CreateSubjectDto, 
  ExamDto, CreateExamDto, 
  RecordResultDto, ResultDto, ReportCardDto,
  ExistingResultDto
} from '../models/academics.models';

@Injectable({ providedIn: 'root' })
export class AcademicsService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  // Subjects
  getSubjectsByClass(classId: number) {
    return this.http.get<SubjectDto[]>(`${this.baseUrl}/subjects/class/${classId}`);
  }

  createSubject(dto: CreateSubjectDto) {
    return this.http.post<SubjectDto>(`${this.baseUrl}/subjects`, dto);
  }

  deleteSubject(id: number) {
    return this.http.delete(`${this.baseUrl}/subjects/${id}`);
  }

  // Exams
  getExams() {
    return this.http.get<ExamDto[]>(`${this.baseUrl}/exams`);
  }

  createExam(dto: CreateExamDto) {
    return this.http.post<ExamDto>(`${this.baseUrl}/exams`, dto);
  }

  updateExam(id: number, dto: CreateExamDto) {
    return this.http.put<ExamDto>(`${this.baseUrl}/exams/${id}`, dto);
  }

  deleteExam(id: number) {
    return this.http.delete(`${this.baseUrl}/exams/${id}`);
  }

  // Results
  recordResult(dto: RecordResultDto) {
    return this.http.post<ResultDto>(`${this.baseUrl}/results`, dto);
  }

  getReportCard(studentId: number, examId: number) {
    return this.http.get<ReportCardDto>(`${this.baseUrl}/students/${studentId}/report-card/${examId}`);
  }

  getExistingResults(examId: number, subjectId: number) {
    return this.http.get<ExistingResultDto[]>(`${this.baseUrl}/results/exam/${examId}/subject/${subjectId}`);
  }
}