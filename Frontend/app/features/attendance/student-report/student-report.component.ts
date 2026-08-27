import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ClassesService } from '../../students/services/classes.service';
import { StudentsService } from '../../students/services/students.service';
import { AttendanceService } from '../services/attendance.service';
import { ClassDto } from '../../students/models/class.models';
import { StudentDto } from '../../students/models/student.models';
import { StudentAttendanceSummary } from '../models/attendance.models';
import { AttendanceNavComponent } from '../../../shared/components/attendance-nav/attendance-nav.component';

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

@Component({
  selector: 'app-student-report',
  standalone: true,
  imports: [FormsModule, CommonModule, AttendanceNavComponent],
  templateUrl: './student-report.component.html',
  styleUrl: './student-report.component.scss'
})
export class StudentReportComponent implements OnInit {
  private classesService = inject(ClassesService);
  private studentsService = inject(StudentsService);
  private attendanceService = inject(AttendanceService);

  monthNames = MONTH_NAMES;
  classes = signal<ClassDto[]>([]);
  allStudents = signal<StudentDto[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedStudentId = signal<number | null>(null);
  selectedMonth = signal<number>(new Date().getMonth() + 1);
  selectedYear = signal<number>(new Date().getFullYear());

  summary = signal<StudentAttendanceSummary | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  filteredStudents = computed(() => {
    const classId = this.selectedClassId();
    if (!classId) return [];
    return this.allStudents().filter((s: any) => s.classId === classId);
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
        }
      },
      error: () => this.error.set('Could not load classes.')
    });

    this.studentsService.getAll().subscribe({
      next: (data: any) => this.allStudents.set(data),
      error: () => this.error.set('Could not load students.')
    });
  }

  onClassChange() {
    this.selectedStudentId.set(null);
    this.summary.set(null);
  }

  onStudentChange() {
    this.loadSummary();
  }

  onMonthChange() { this.loadSummary(); }
  onYearChange() { this.loadSummary(); }

  printReport() {
    window.print();
  }

  loadSummary() {
    const studentId = this.selectedStudentId();
    if (!studentId) return;

    this.loading.set(true);
    this.error.set(null);
    this.attendanceService.getStudentSummary(studentId, this.selectedMonth(), this.selectedYear()).subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.summary.set(null);
        if (err.status === 404) {
          this.error.set('No attendance data found for this period.');
        } else {
          this.error.set('Could not load attendance summary.');
        }
        this.loading.set(false);
      }
    });
  }
}
