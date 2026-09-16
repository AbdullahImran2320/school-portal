import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { StudentDto, CreateStudentDto, UpdateStudentDto } from '../models/student.models';
import { StudentProfile } from '../profile/profile.models';

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

  getProfile(id: number) {
    return this.http.get<StudentProfile>(`${this.baseUrl}/${id}/profile`);
  }

  // A plain <img src> can't carry the auth header this endpoint requires,
  // so the photo is fetched as a blob through HttpClient (same pattern as
  // backup downloads) and handed to the template as an object URL.
  getPhoto(id: number) {
    return this.http.get(`${this.baseUrl}/${id}/photo`, { responseType: 'blob' });
  }

  uploadPhoto(id: number, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ photoFileName: string }>(`${this.baseUrl}/${id}/photo`, formData);
  }

  deletePhoto(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}/photo`);
  }

  create(dto: CreateStudentDto) {
    return this.http.post<StudentDto>(this.baseUrl, dto);
  }

  update(id: number, dto: UpdateStudentDto) {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }

  // Deliberately separate from update() — matches the backend's own
  // separation: assigning a roll number is its own action with its own
  // uniqueness check, not a side effect of an unrelated field edit.
  setRollNumber(id: number, rollNumber: number) {
    return this.http.put<void>(`${this.baseUrl}/${id}/roll-number`, { rollNumber });
  }

  delete(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}