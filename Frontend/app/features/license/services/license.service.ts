import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { Observable, catchError, of, shareReplay } from 'rxjs';
import { LicenseActivationResponse, LicenseStatus } from '../models/license.models';

@Injectable({ providedIn: 'root' })
export class LicenseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/license`;

  getStatus(): Observable<LicenseStatus> {
    return this.http.get<LicenseStatus>(`${this.baseUrl}/status`).pipe(
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }

  activate(licenseKey: string): Observable<LicenseActivationResponse> {
    return this.http.post<LicenseActivationResponse>(`${this.baseUrl}/activate`, {
      licenseKey: licenseKey.trim()
    });
  }

  /**
   * Convenience helper for non-blocking UI checks.
   * The backend remains the authority for license enforcement.
   */
  getStatusSafe(): Observable<LicenseStatus | null> {
    return this.getStatus().pipe(
      catchError(() => of(null))
    );
  }
}
