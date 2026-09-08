import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FeeVoucher, PaidReceipt, ChallanSettings } from '../models/fee.models';

export interface VoucherFilters {
  chargeType?: string;
  admissionDateFrom?: string; // yyyy-MM-dd
  admissionDateTo?: string;
}

@Injectable({ providedIn: 'root' })
export class VouchersService {
  private http = inject(HttpClient);

  getClassVouchers(classId: number, month: number, year: number, filters?: VoucherFilters) {
    const params: Record<string, string | number> = { month, year };
    if (filters?.chargeType) params['chargeType'] = filters.chargeType;
    if (filters?.admissionDateFrom) params['admissionDateFrom'] = filters.admissionDateFrom;
    if (filters?.admissionDateTo) params['admissionDateTo'] = filters.admissionDateTo;
    return this.http.get<FeeVoucher[]>(`${environment.apiUrl}/classes/${classId}/vouchers`, { params });
  }

  getChargeTypesForClass(classId: number) {
    return this.http.get<string[]>(`${environment.apiUrl}/classes/${classId}/charge-types`);
  }

  getClassReceipts(classId: number, month: number, year: number) {
    return this.http.get<PaidReceipt[]>(`${environment.apiUrl}/classes/${classId}/receipts`, { params: { month, year } });
  }

  getChallanSettings() {
    return this.http.get<ChallanSettings>(`${environment.apiUrl}/settings/challan`);
  }

  updateChallanSettings(dto: ChallanSettings) {
    return this.http.put<ChallanSettings>(`${environment.apiUrl}/settings/challan`, dto);
  }
}
