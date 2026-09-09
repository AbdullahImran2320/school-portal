import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ClassFeeGrid, FeeSummary, Defaulter, RecordPaymentRequest, PaymentResult, SetStudentDiscountRequest, CollectionSummary } from '../models/fee.models';

@Injectable({ providedIn: 'root' })
export class FeesService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  getClassFeeGrid(classId: number, year: number) {
    return this.http.get<ClassFeeGrid>(`${this.baseUrl}/classes/${classId}/fee-grid`, { params: { year } });
  }

  getStudentFeeSummary(studentId: number) {
    return this.http.get<FeeSummary>(`${this.baseUrl}/students/${studentId}/fee-summary`);
  }

  getDefaulters() {
    return this.http.get<Defaulter[]>(`${this.baseUrl}/reports/defaulters`);
  }

  exportDefaulters() {
    return this.http.get(`${this.baseUrl}/reports/defaulters/export`, { responseType: 'blob' });
  }

  getCollectionSummary(month: number, year: number) {
    return this.http.get<CollectionSummary>(`${this.baseUrl}/reports/collection-summary`, { params: { month, year } });
  }

  exportCollectionSummary(month: number, year: number) {
    return this.http.get(`${this.baseUrl}/reports/collection-summary/export`, { params: { month, year }, responseType: 'blob' });
  }

  payLedger(ledgerId: number, dto: RecordPaymentRequest) {
    return this.http.post<PaymentResult>(`${this.baseUrl}/payments/ledger/${ledgerId}`, dto);
  }

  payCharge(chargeId: number, dto: RecordPaymentRequest) {
    return this.http.post<PaymentResult>(`${this.baseUrl}/payments/charge/${chargeId}`, dto);
  }

  setStudentDiscount(studentId: number, dto: SetStudentDiscountRequest) {
    return this.http.put<void>(`${this.baseUrl}/students/${studentId}/discount`, dto);
  }
}