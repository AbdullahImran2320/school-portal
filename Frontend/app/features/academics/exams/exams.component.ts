import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AcademicsNavComponent } from '../../../shared/components/academics-nav/academics-nav.component';
import { AcademicsService } from '../services/academics.service';
import { ExamDto, CreateExamDto } from '../models/academics.models';

@Component({
  selector: 'app-exams',
  standalone: true,
  imports: [CommonModule, FormsModule, AcademicsNavComponent],
  templateUrl: './exams.component.html',
  styleUrl: './exams.component.scss'
})
export class ExamsComponent implements OnInit {
  private academicsService = inject(AcademicsService);

  exams = signal<ExamDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  actionError = signal<string | null>(null);

  // Add Exam Form
  newExam = signal<CreateExamDto>({ examName: '', term: '1st Term', academicYear: new Date().getFullYear().toString() });
  adding = signal(false);

  // Edit Exam Form
  editingExamId = signal<number | null>(null);
  editDraft = signal<CreateExamDto>({ examName: '', term: '1st Term', academicYear: '' });
  saving = signal(false);
  deletingId = signal<number | null>(null);

  ngOnInit() {
    this.loadExams();
  }

  loadExams() {
    this.loading.set(true);
    this.error.set(null);
    this.academicsService.getExams().subscribe({
      next: (data) => {
        this.exams.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load exams.');
        this.loading.set(false);
      }
    });
  }

  addExam() {
    const dto = this.newExam();
    if (!dto.examName.trim()) return;

    this.actionError.set(null);
    this.adding.set(true);
    this.academicsService.createExam(dto).subscribe({
      next: (newEx) => {
        this.exams.update(exams => [...exams, newEx]);
        this.newExam.set({ examName: '', term: '1st Term', academicYear: new Date().getFullYear().toString() });
        this.adding.set(false);
      },
      error: () => {
        this.actionError.set('Failed to add exam. Name must be at least 2 characters.');
        this.adding.set(false);
      }
    });
  }

  startEdit(exam: ExamDto) {
    this.editingExamId.set(exam.examId);
    this.editDraft.set({ examName: exam.examName, term: exam.term, academicYear: exam.academicYear });
  }

  cancelEdit() {
    this.editingExamId.set(null);
  }

  saveEdit(id: number) {
    const dto = this.editDraft();
    if (!dto.examName.trim()) return;

    this.actionError.set(null);
    this.saving.set(true);
    this.academicsService.updateExam(id, dto).subscribe({
      next: (updated) => {
        this.exams.update(exams => exams.map(e => e.examId === id ? updated : e));
        this.editingExamId.set(null);
        this.saving.set(false);
      },
      error: () => {
        this.actionError.set('Failed to update exam. Name must be at least 2 characters.');
        this.saving.set(false);
      }
    });
  }

  deleteExam(exam: ExamDto) {
    if (!confirm(`Are you sure you want to delete "${exam.examName}"?`)) return;

    this.actionError.set(null);
    this.deletingId.set(exam.examId);
    this.academicsService.deleteExam(exam.examId).subscribe({
      next: () => {
        this.exams.update(exams => exams.filter(e => e.examId !== exam.examId));
        this.deletingId.set(null);
      },
      error: (err) => {
        if (err.status === 409) {
          this.actionError.set(err.error?.message ?? 'This exam has recorded results and cannot be deleted.');
        } else {
          this.actionError.set('Failed to delete exam.');
        }
        this.deletingId.set(null);
      }
    });
  }
}
