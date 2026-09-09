import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { BackupsService } from './backups.service';
import { BackupInfo } from './backups.models';

@Component({
  selector: 'app-backups',
  standalone: true,
  imports: [AdminNavComponent, DatePipe ],
  templateUrl: './backups.component.html',
  styleUrl: './backups.component.scss'
})
export class BackupsComponent implements OnInit {
  private backupsService = inject(BackupsService);

  backups = signal<BackupInfo[]>([]);
  loading = signal(false);
  creating = signal(false);
  downloadingFile = signal<string | null>(null);
  error = signal<string | null>(null);

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.backupsService.list().subscribe({
      next: (data) => { this.backups.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load backups.'); this.loading.set(false); }
    });
  }

  backupNow() {
    this.creating.set(true);
    this.error.set(null);
    this.backupsService.createNow().subscribe({
      next: () => { this.creating.set(false); this.load(); },
      error: (err) => {
        this.error.set(err?.error ?? 'Backup failed.');
        this.creating.set(false);
      }
    });
  }

  download(backup: BackupInfo) {
    this.downloadingFile.set(backup.fileName);
    this.backupsService.download(backup.fileName).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = backup.fileName;
        link.click();
        window.URL.revokeObjectURL(url);
        this.downloadingFile.set(null);
      },
      error: () => {
        this.error.set(`Could not download ${backup.fileName}.`);
        this.downloadingFile.set(null);
      }
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
