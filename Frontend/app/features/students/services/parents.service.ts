import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ParentDto, UpsertParentDto } from '../models/parent.models';

@Injectable({ providedIn: 'root' })
export class ParentsService {
  private baseUrl = `${environment.apiUrl}/parents`;

  constructor(private http: HttpClient) {}

  searchByMobile(mobile: string) {
    return this.http.get<ParentDto[]>(`${this.baseUrl}/search`, { params: { mobile } });
  }

  create(dto: UpsertParentDto) {
    return this.http.post<ParentDto>(this.baseUrl, dto);
  }
}