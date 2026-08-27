import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { AdminService } from '../services/admin.service';
import { AuditLogDto, AuditLogFilter } from '../models/admin.models';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminNavComponent],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss'
})
export class AuditLogComponent implements OnInit {
  private adminService = inject(AdminService);

  logs = signal<AuditLogDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  filterEntityName = signal('');
  filterEntityId = signal('');
  filterFrom = signal('');
  filterTo = signal('');

  // Common entity names in this system — a free-text field also works,
  // this is just a convenience shortcut since it covers most lookups.
  knownEntities = ['Student', 'Parent', 'SchoolClass', 'Subject', 'Exam', 'Result', 'FeeComponent', 'StudentCharge', 'FeeLedger', 'Payment', 'Attendance', 'User'];

  ngOnInit() {
    this.loadLogs();
  }

  loadLogs() {
    this.loading.set(true);
    this.error.set(null);

    const filter: AuditLogFilter = {
      entityName: this.filterEntityName() || undefined,
      entityId: this.filterEntityId().trim() || undefined,
      from: this.filterFrom() ? new Date(this.filterFrom()).toISOString() : undefined,
      // "to" is a date picker (no time component), so treat it as end-of-day —
      // otherwise picking "today" would exclude every event from today itself.
      to: this.filterTo() ? new Date(this.filterTo() + 'T23:59:59.999').toISOString() : undefined
    };

    this.adminService.getAuditLogs(filter).subscribe({
      next: (data) => {
        this.logs.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load the audit log.');
        this.loading.set(false);
      }
    });
  }

  applyFilters() {
    this.loadLogs();
  }

  clearFilters() {
    this.filterEntityName.set('');
    this.filterEntityId.set('');
    this.filterFrom.set('');
    this.filterTo.set('');
    this.loadLogs();
  }

  actionClass(action: string): string {
    const a = action.toLowerCase();
    if (a.includes('add')) return 'action-added';
    if (a.includes('delet')) return 'action-deleted';
    if (a.includes('modif')) return 'action-modified';
    return '';
  }
}
