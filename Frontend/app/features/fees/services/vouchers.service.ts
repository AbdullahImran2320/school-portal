import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { FeeVoucher, PaidReceipt } from '../models/fee.models';

@Injectable({ providedIn: 'root' })
export class VouchersService {
  private http = inject(HttpClient);

  getClassVouchers(classId: number, month: number, year: number) {
    return this.http.get<FeeVoucher[]>(`${environment.apiUrl}/classes/${classId}/vouchers`, { params: { month, year } });
  }

  getClassReceipts(classId: number, month: number, year: number) {
    return this.http.get<PaidReceipt[]>(`${environment.apiUrl}/classes/${classId}/receipts`, { params: { month, year } });
  }
}