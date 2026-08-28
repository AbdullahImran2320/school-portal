import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { AdminService } from '../services/admin.service';
import { StudentsService } from '../../students/services/students.service';
import { ClassesService } from '../../students/services/classes.service';
import { StudentDto } from '../../students/models/student.models';
import { ClassDto } from '../../students/models/class.models';
import { PromotionResultDto } from '../models/admin.models';

interface ClassGroup {
  classId: number;
  className: string;
  promotionOrder: number;
  students: StudentDto[];
}

@Component({
  selector: 'app-promotion',
  standalone: true,
  imports: [CommonModule, FormsModule, AdminNavComponent],
  templateUrl: './promotion.component.html',
  styleUrl: './promotion.component.scss'
})
export class PromotionComponent implements OnInit {
  private adminService = inject(AdminService);
  private studentsService = inject(StudentsService);
  private classesService = inject(ClassesService);

  fromAcademicYear = signal<string>(String(new Date().getFullYear()));
  toAcademicYear = signal<string>(String(new Date().getFullYear() + 1));

  classes = signal<ClassDto[]>([]);
  students = signal<StudentDto[]>([]);
  holdBackIds = signal<Set<number>>(new Set());

  loading = signal(false);
  error = signal<string | null>(null);
  submitting = signal(false);
  result = signal<PromotionResultDto | null>(null);
  confirmOpen = signal(false);

  admittedStudents = computed(() => this.students().filter(s => s.admissionStatus === 'Admitted'));

  groupedByClass = computed<ClassGroup[]>(() => {
    const classMap = new Map(this.classes().map(c => [c.classId, c]));
    const groups = new Map<number, ClassGroup>();

    for (const s of this.admittedStudents()) {
      const cls = classMap.get(s.classId);
      if (!groups.has(s.classId)) {
        groups.set(s.classId, {
          classId: s.classId,
          className: cls?.className ?? s.className,
          promotionOrder: cls?.promotionOrder ?? 0,
          students: []
        });
      }
      groups.get(s.classId)!.students.push(s);
    }

    return [...groups.values()].sort((a, b) => a.promotionOrder - b.promotionOrder);
  });

  totalAdmitted = computed(() => this.admittedStudents().length);
  holdBackCount = computed(() => this.holdBackIds().size);
  toBePromotedCount = computed(() => this.totalAdmitted() - this.holdBackCount());

  ngOnInit() {
    this.loading.set(true);
    this.error.set(null);

    this.classesService.getAll().subscribe({
      next: (data) => this.classes.set(data),
      error: () => this.error.set('Could not load classes.')
    });

    this.studentsService.getAll().subscribe({
      next: (data) => {
        this.students.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load students.');
        this.loading.set(false);
      }
    });
  }

  isHeldBack(studentId: number): boolean {
    return this.holdBackIds().has(studentId);
  }

  toggleHoldBack(studentId: number) {
    this.holdBackIds.update(set => {
      const next = new Set(set);
      if (next.has(studentId)) next.delete(studentId);
      else next.add(studentId);
      return next;
    });
  }

  openConfirm() {
    const from = this.fromAcademicYear().trim();
    const to = this.toAcademicYear().trim();
    if (!from || !to) {
      this.error.set('Enter both the From and To academic year.');
      return;
    }
    if (from === to) {
      this.error.set('From and To academic year must be different.');
      return;
    }
    this.error.set(null);
    this.result.set(null);
    this.confirmOpen.set(true);
  }

  closeConfirm() {
    if (this.submitting()) return;
    this.confirmOpen.set(false);
  }

  runPromotion() {
    this.submitting.set(true);
    this.error.set(null);

    this.adminService.promoteYear({
      fromAcademicYear: this.fromAcademicYear().trim(),
      toAcademicYear: this.toAcademicYear().trim(),
      holdBackStudentIds: [...this.holdBackIds()]
    }).subscribe({
      next: (res) => {
        this.result.set(res);
        this.submitting.set(false);
        this.confirmOpen.set(false);
        this.holdBackIds.set(new Set());
        // Refresh so students who were promoted immediately show their new class
        this.studentsService.getAll().subscribe(data => this.students.set(data));
      },
      error: (err) => {
        this.submitting.set(false);
        this.error.set(err?.error?.message ?? 'Could not run promotion. Please try again.');
      }
    });
  }
}
