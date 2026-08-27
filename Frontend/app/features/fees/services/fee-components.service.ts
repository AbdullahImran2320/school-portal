import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FeeComponent, UpsertFeeComponent } from '../models/fee.models';

@Injectable({ providedIn: 'root' })
export class FeeComponentsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/feecomponents`;

  getByClass(classId: number) {
    return this.http.get<FeeComponent[]>(`${this.baseUrl}/class/${classId}`);
  }
  create(dto: UpsertFeeComponent) {
    return this.http.post<FeeComponent>(this.baseUrl, dto);
  }
  update(id: number, dto: UpsertFeeComponent) {
    return this.http.put<void>(`${this.baseUrl}/${id}`, dto);
  }
  delete(id: number) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
  rollover(fromYear: string, toYear: string) {
    return this.http.post<{ message: string }>(`${this.baseUrl}/rollover`, null, { params: { fromYear, toYear } });
  }
}