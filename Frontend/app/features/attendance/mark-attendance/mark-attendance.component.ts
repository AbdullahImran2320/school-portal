import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ClassesService } from '../../students/services/classes.service';
import { AttendanceService } from '../services/attendance.service';
import { ClassDto } from '../../students/models/class.models';
import { ClassAttendanceRow, AttendanceStatus } from '../models/attendance.models';
import { AttendanceNavComponent } from '../../../shared/components/attendance-nav/attendance-nav.component';

@Component({
  selector: 'app-mark-attendance',
  standalone: true,
  imports: [FormsModule, CommonModule, AttendanceNavComponent],
  templateUrl: './mark-attendance.component.html',
  styleUrl: './mark-attendance.component.scss'
})
export class MarkAttendanceComponent implements OnInit {
  private classesService = inject(ClassesService);
  private attendanceService = inject(AttendanceService);

  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedDate = signal<string>(this.todayString());
  rows = signal<ClassAttendanceRow[]>([]);
  loading = signal(false);
  submitting = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  statuses: AttendanceStatus[] = ['Present', 'Absent', 'Leave', 'Late'];

  markedCount = computed(() => this.rows().filter(r => r.status !== 'NotMarked').length);
  totalCount = computed(() => this.rows().length);

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => {
        this.classes.set(data);
        if (data.length > 0) {
          this.selectedClassId.set(data[0].classId);
          this.loadAttendance();
        }
      },
      error: () => this.error.set('Could not load classes.')
    });
  }

  onClassChange() { this.loadAttendance(); }
  onDateChange() { this.loadAttendance(); }

  loadAttendance() {
    const classId = this.selectedClassId();
    const date = this.selectedDate();
    if (!classId || !date) return;

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);
    this.attendanceService.getClassAttendance(classId, date).subscribe({
      next: (data) => {
        this.rows.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load attendance.');
        this.loading.set(false);
      }
    });
  }

  setStatus(studentId: number, status: AttendanceStatus) {
    this.rows.update(rows =>
      rows.map(r => r.studentId === studentId ? { ...r, status } : r)
    );
  }

  setAll(status: AttendanceStatus) {
    this.rows.update(rows => rows.map(r => ({ ...r, status })));
  }

  submit() {
    const classId = this.selectedClassId();
    const date = this.selectedDate();
    if (!classId || !date) return;

    const entries = this.rows()
      .filter(r => r.status !== 'NotMarked')
      .map(r => ({ studentId: r.studentId, status: r.status }));

    if (entries.length === 0) {
      this.error.set('Please mark at least one student.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.success.set(null);

    this.attendanceService.markBulk({ classId, date, entries }).subscribe({
      next: (res) => {
        this.success.set(res.message);
        this.submitting.set(false);
      },
      error: () => {
        this.error.set('Failed to save attendance.');
        this.submitting.set(false);
      }
    });
  }

  private todayString(): string {
    const d = new Date();
    return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
  }
}
