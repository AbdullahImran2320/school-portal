import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { BackupInfo } from './backups.models';

@Injectable({ providedIn: 'root' })
export class BackupsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/backups`;

  list() {
    return this.http.get<BackupInfo[]>(this.baseUrl);
  }

  createNow() {
    return this.http.post<BackupInfo>(this.baseUrl, {});
  }

  // A plain <a href> download would skip the auth interceptor entirely
  // (browsers don't attach it to plain navigations), and this endpoint
  // requires an Admin token — so the file is fetched as a blob through
  // HttpClient (which does carry the auth header) and then handed to the
  // browser as a temporary object URL to trigger the actual download.
  download(fileName: string) {
    return this.http.get(`${this.baseUrl}/${fileName}/download`, { responseType: 'blob' });
  }
}
