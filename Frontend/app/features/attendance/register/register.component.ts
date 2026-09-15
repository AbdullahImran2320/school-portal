import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ClassesService } from '../../students/services/classes.service';
import { AttendanceService } from '../services/attendance.service';
import { ClassDto } from '../../students/models/class.models';
import { AttendanceRegister, AttendanceStatus } from '../models/attendance.models';
import { AttendanceNavComponent } from '../../../shared/components/attendance-nav/attendance-nav.component';
import { downloadBlob } from '../../../shared/utils/download-file';

const MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];

// Single-letter codes for the grid — a whole month has to fit across the
// screen/page legibly, matching how a physical attendance register looks.
const STATUS_LETTER: Record<AttendanceStatus, string> = {
  Present: 'P', Absent: 'A', Leave: 'L', Late: 'T', NotMarked: ''
};

@Component({
  selector: 'app-attendance-register',
  standalone: true,
  imports: [FormsModule, CommonModule, AttendanceNavComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss'
})
export class AttendanceRegisterComponent implements OnInit {
  private classesService = inject(ClassesService);
  private attendanceService = inject(AttendanceService);

  monthNames = MONTH_NAMES;
  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedMonth = signal(new Date().getMonth() + 1);
  selectedYear = signal(new Date().getFullYear());

  register = signal<AttendanceRegister | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  exporting = signal(false);
  exportError = signal<string | null>(null);

  dayNumbers = computed(() => {
    const r = this.register();
    return r ? Array.from({ length: r.daysInMonth }, (_, i) => i + 1) : [];
  });

  yearOptions = computed(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1];
  });

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => {
        this.classes.set(data);
        if (data.length > 0) {
          this.selectedClassId.set(data[0].classId);
          this.load();
        }
      },
      error: () => this.error.set('Could not load classes.')
    });
  }

  load() {
    const classId = this.selectedClassId();
    if (!classId) return;

    this.loading.set(true);
    this.error.set(null);
    this.exportError.set(null);
    this.attendanceService.getClassRegister(classId, this.selectedMonth(), this.selectedYear()).subscribe({
      next: (data) => { this.register.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load the attendance register.'); this.loading.set(false); }
    });
  }

  letterFor(status: AttendanceStatus): string {
    return STATUS_LETTER[status] ?? '';
  }

  exportToExcel() {
    const classId = this.selectedClassId();
    if (!classId) return;

    this.exportError.set(null);
    this.exporting.set(true);
    this.attendanceService.exportClassRegister(classId, this.selectedMonth(), this.selectedYear()).subscribe({
      next: (blob) => {
        const monthTag = `${this.selectedYear()}-${String(this.selectedMonth()).padStart(2, '0')}`;
        downloadBlob(blob, `Attendance-${monthTag}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exportError.set('Could not export the attendance register.');
        this.exporting.set(false);
      }
    });
  }
}
