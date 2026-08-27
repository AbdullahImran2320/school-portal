import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ClassDto } from '../models/class.models';

@Injectable({ providedIn: 'root' })
export class ClassesService {
  private baseUrl = `${environment.apiUrl}/classes`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<ClassDto[]>(this.baseUrl);
  }
}