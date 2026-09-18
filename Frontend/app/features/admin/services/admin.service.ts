import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { AppRole, AuditLogDto, AuditLogFilter, UserSummaryDto, PromoteClassesDto, PromotionResultDto, RollNumberSettingsDto } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  // Users
  getUsers() {
    return this.http.get<UserSummaryDto[]>(`${this.baseUrl}/users`);
  }

  updateUserRole(id: number, role: AppRole) {
    return this.http.put(`${this.baseUrl}/users/${id}/role`, { role });
  }

  // Audit Log
  getAuditLogs(filter: AuditLogFilter) {
    let params = new HttpParams();
    if (filter.entityName) params = params.set('entityName', filter.entityName);
    if (filter.entityId) params = params.set('entityId', filter.entityId);
    if (filter.from) params = params.set('from', filter.from);
    if (filter.to) params = params.set('to', filter.to);

    return this.http.get<AuditLogDto[]>(`${this.baseUrl}/auditlogs`, { params });
  }

  // Promotion
  promoteYear(dto: PromoteClassesDto) {
    return this.http.post<PromotionResultDto>(`${this.baseUrl}/promotion/promote-year`, dto);
  }

  // Roll Number Settings — the globally-editable prefix/padding; each
  // class's own code lives on the class row (see ClassesService).
  getRollNumberSettings() {
    return this.http.get<RollNumberSettingsDto>(`${this.baseUrl}/settings/roll-number`);
  }

  updateRollNumberSettings(dto: RollNumberSettingsDto) {
    return this.http.put<RollNumberSettingsDto>(`${this.baseUrl}/settings/roll-number`, dto);
  }
}
