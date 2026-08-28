import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AcademicsNavComponent } from '../../../shared/components/academics-nav/academics-nav.component';
import { ClassesService } from '../../students/services/classes.service';
import { StudentsService } from '../../students/services/students.service';
import { AcademicsService } from '../services/academics.service';
import { ClassDto } from '../../students/models/class.models';
import { StudentDto } from '../../students/models/student.models';
import { ExamDto, SubjectDto, RecordResultDto } from '../models/academics.models';

interface ResultRow {
  studentId: number;
  studentName: string;
  marksObtained: number | null;
  totalMarks: number | null;
  status: 'idle' | 'saving' | 'saved' | 'error';
}

@Component({
  selector: 'app-record-results',
  standalone: true,
  imports: [CommonModule, FormsModule, AcademicsNavComponent],
  templateUrl: './record-results.component.html',
  styleUrl: './record-results.component.scss'
})
export class RecordResultsComponent implements OnInit {
  private classesService = inject(ClassesService);
  private studentsService = inject(StudentsService);
  private academicsService = inject(AcademicsService);

  classes = signal<ClassDto[]>([]);
  exams = signal<ExamDto[]>([]);
  subjects = signal<SubjectDto[]>([]);
  students = signal<StudentDto[]>([]);
  
  selectedClassId = signal<number | null>(null);
  selectedExamId = signal<number | null>(null);
  selectedSubjectId = signal<number | null>(null);

  rows = signal<ResultRow[]>([]);
  globalTotalMarks = signal<number | null>(null);
  
  loading = signal(false);
  error = signal<string | null>(null);

  ngOnInit() {
    this.classesService.getAll().subscribe(data => {
      this.classes.set(data);
    });
    this.academicsService.getExams().subscribe(data => {
      this.exams.set(data);
    });
  }

  onClassChange() {
    this.selectedSubjectId.set(null);
    this.subjects.set([]);
    this.rows.set([]);
    const classId = this.selectedClassId();
    if (classId) {
      this.academicsService.getSubjectsByClass(classId).subscribe(data => this.subjects.set(data));
      this.loadStudents(classId);
    }
  }

  onExamChange() {
    this.buildRows();
  }

  onSubjectChange() {
    this.buildRows();
  }

  loadStudents(classId: number) {
    this.loading.set(true);
    // Use student service. We can filter clientside or call an endpoint.
    this.studentsService.getAll().subscribe({
      next: (data: any) => {
        const classStudents = data.filter((s: any) => s.classId === classId);
        this.students.set(classStudents);
        this.buildRows();
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load students.');
        this.loading.set(false);
      }
    });
  }

  buildRows() {
    const classId = this.selectedClassId();
    const examId = this.selectedExamId();
    const subjectId = this.selectedSubjectId();

    if (!classId || !examId || !subjectId || this.students().length === 0) {
      this.rows.set([]);
      return;
    }

    const newRows: ResultRow[] = this.students().map(s => ({
      studentId: s.studentId,
      studentName: s.name,
      marksObtained: null,
      totalMarks: this.globalTotalMarks(),
      status: 'idle' as const
    }));
    this.rows.set(newRows);

    // Pre-fill any marks already recorded for this exam/subject, so
    // reopening the page (or fixing a typo) edits the existing result
    // instead of the teacher unknowingly entering a duplicate.
    this.academicsService.getExistingResults(examId, subjectId).subscribe({
      next: existing => {
        if (existing.length === 0) return;
        const byStudent = new Map(existing.map(e => [e.studentId, e]));
        this.rows.update(rs => rs.map(r => {
          const match = byStudent.get(r.studentId);
          return match
            ? { ...r, marksObtained: match.marksObtained, totalMarks: match.totalMarks, status: 'saved' as const }
            : r;
        }));
      },
      error: () => {
        // Non-fatal: the teacher can still enter marks fresh; saveResult()
        // now upserts server-side regardless of whether this lookup worked.
      }
    });
  }

  applyGlobalTotal() {
    const total = this.globalTotalMarks();
    if (total != null) {
      this.rows.update(rs => rs.map(r => ({ ...r, totalMarks: total })));
    }
  }

  saveResult(row: ResultRow) {
    if (row.marksObtained == null || row.totalMarks == null || row.totalMarks <= 0) return;
    
    const dto: RecordResultDto = {
      studentId: row.studentId,
      examId: this.selectedExamId()!,
      subjectId: this.selectedSubjectId()!,
      marksObtained: row.marksObtained,
      totalMarks: row.totalMarks
    };

    this.updateRowStatus(row.studentId, 'saving');
    
    this.academicsService.recordResult(dto).subscribe({
      next: () => this.updateRowStatus(row.studentId, 'saved'),
      error: () => this.updateRowStatus(row.studentId, 'error')
    });
  }

  private updateRowStatus(studentId: number, status: ResultRow['status']) {
    this.rows.update(rs => rs.map(r => r.studentId === studentId ? { ...r, status } : r));
  }
}