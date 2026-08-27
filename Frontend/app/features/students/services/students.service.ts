import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { StudentDto, CreateStudentDto, UpdateStudentDto } from '../models/student.models';

@Injectable({ providedIn: 'root' })
export class StudentsService {
  private baseUrl = `${environment.apiUrl}/students`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<StudentDto[]>(this.baseUrl);
  }

  getById(id: number) {
    return this.http.get<StudentDto>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateStudentDto) {
    return this.http.post<StudentDto>(this.baseUrl, dto);
  }

  update(id: number, dto: UpdateStudentDto) {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}