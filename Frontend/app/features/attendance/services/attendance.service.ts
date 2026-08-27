import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { BulkMarkAttendance, ClassAttendanceRow, StudentAttendanceSummary } from '../models/attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/attendance`;

  markBulk(dto: BulkMarkAttendance) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/mark`, dto);
  }

  getClassAttendance(classId: number, date: string) {
    return this.http.get<ClassAttendanceRow[]>(`${this.baseUrl}/class/${classId}`, { params: { date } });
  }

  getStudentSummary(studentId: number, month: number, year: number) {
    return this.http.get<StudentAttendanceSummary>(`${this.baseUrl}/students/${studentId}/summary`, { params: { month, year } });
  }
}
