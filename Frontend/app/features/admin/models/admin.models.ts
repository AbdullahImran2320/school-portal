export type AppRole = 'Pending' | 'Teacher' | 'Accountant' | 'Admin';

export const ASSIGNABLE_ROLES: AppRole[] = ['Pending', 'Teacher', 'Accountant', 'Admin'];

export interface UserSummaryDto {
  userId: number;
  username: string;
  fullName: string;
  role: AppRole;
}

export interface UpdateRoleDto {
  role: AppRole;
}

export interface AuditLogDto {
  auditLogId: number;
  entityName: string;
  entityId: string;
  action: string;
  changedBy: string;
  timestamp: string;
  details: string;
}

export interface AuditLogFilter {
  entityName?: string;
  entityId?: string;
  from?: string;
  to?: string;
}

export interface PromoteClassesDto {
  fromAcademicYear: string;
  toAcademicYear: string;
  holdBackStudentIds: number[];
}

export interface PromotionResultDto {
  promotedCount: number;
  graduatedCount: number;
  heldBackCount: number;
  alreadyProcessedCount: number;
  errors: string[];
}
