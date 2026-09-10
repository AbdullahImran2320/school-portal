import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { StudentImportResult } from './import.models';

@Injectable({ providedIn: 'root' })
export class StudentImportApiService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/students`;

  downloadTemplate() {
    return this.http.get(`${this.baseUrl}/import/template`, { responseType: 'blob' });
  }

  import(file: File) {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<StudentImportResult>(`${this.baseUrl}/import`, formData);
  }
}
