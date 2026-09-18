import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import {
  ClassDto,
  ClassGroupDto,
  CreateClassDto,
  AddSectionDto,
  UpdateClassCodeDto,
  SectionOptionDto,
  CreateSectionOptionDto
} from '../models/class.models';

@Injectable({ providedIn: 'root' })
export class ClassesService {
  private baseUrl = `${environment.apiUrl}/classes`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<ClassDto[]>(this.baseUrl);
  }

  getGrouped() {
    return this.http.get<ClassGroupDto[]>(`${this.baseUrl}/grouped`);
  }

  createClass(dto: CreateClassDto) {
    return this.http.post<ClassDto>(this.baseUrl, dto);
  }

  addSection(dto: AddSectionDto) {
    return this.http.post<ClassDto>(`${this.baseUrl}/sections`, dto);
  }

  updateClassCode(classId: number, dto: UpdateClassCodeDto) {
    return this.http.put<ClassDto>(`${this.baseUrl}/${classId}/code`, dto);
  }

  deleteSection(classId: number) {
    return this.http.delete(`${this.baseUrl}/${classId}`);
  }

  deleteClassGroup(className: string, academicYear: string) {
    const params = new URLSearchParams({ className, academicYear });
    return this.http.delete(`${this.baseUrl}/group?${params.toString()}`);
  }

  getSectionOptions() {
    return this.http.get<SectionOptionDto[]>(`${this.baseUrl}/section-options`);
  }

  addSectionOption(dto: CreateSectionOptionDto) {
    return this.http.post<SectionOptionDto>(`${this.baseUrl}/section-options`, dto);
  }

  deleteSectionOption(sectionOptionId: number) {
    return this.http.delete(`${this.baseUrl}/section-options/${sectionOptionId}`);
  }

  moveStudents(fromClassId: number, toClassId: number) {
    return this.http.post<{ movedCount: number; fromLabel: string; toLabel: string }>(
      `${this.baseUrl}/${fromClassId}/move-students`,
      { toClassId }
    );
  }
}