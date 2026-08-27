import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule, DecimalPipe } from '@angular/common';
import { AcademicsNavComponent } from '../../../shared/components/academics-nav/academics-nav.component';
import { ClassesService } from '../../students/services/classes.service';
import { StudentsService } from '../../students/services/students.service';
import { AcademicsService } from '../services/academics.service';
import { ClassDto } from '../../students/models/class.models';
import { StudentDto } from '../../students/models/student.models';
import { ExamDto, ReportCardDto } from '../models/academics.models';

@Component({
  selector: 'app-report-card',
  standalone: true,
  imports: [CommonModule, FormsModule, AcademicsNavComponent, DecimalPipe],
  templateUrl: './report-card.component.html',
  styleUrl: './report-card.component.scss'
})
export class ReportCardComponent implements OnInit {
  private classesService = inject(ClassesService);
  private studentsService = inject(StudentsService);
  private academicsService = inject(AcademicsService);

  classes = signal<ClassDto[]>([]);
  allStudents = signal<StudentDto[]>([]);
  exams = signal<ExamDto[]>([]);
  
  selectedClassId = signal<number | null>(null);
  selectedStudentId = signal<number | null>(null);
  selectedExamId = signal<number | null>(null);

  reportCard = signal<ReportCardDto | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);

  filteredStudents = computed(() => {
    const classId = this.selectedClassId();
    if (!classId) return [];
    return this.allStudents().filter((s: any) => s.classId === classId);
  });

  ngOnInit() {
    this.classesService.getAll().subscribe(data => this.classes.set(data));
    this.studentsService.getAll().subscribe((data: any) => this.allStudents.set(data));
    this.academicsService.getExams().subscribe(data => this.exams.set(data));
  }

  onClassChange() {
    this.selectedStudentId.set(null);
    this.reportCard.set(null);
  }

  onStudentChange() {
    this.loadReportCard();
  }

  onExamChange() {
    this.loadReportCard();
  }

  loadReportCard() {
    const studentId = this.selectedStudentId();
    const examId = this.selectedExamId();
    if (!studentId || !examId) return;

    this.loading.set(true);
    this.error.set(null);
    this.academicsService.getReportCard(studentId, examId).subscribe({
      next: (data) => {
        this.reportCard.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.reportCard.set(null);
        if (err.status === 404) {
          this.error.set('Report card not found or results not fully recorded.');
        } else {
          this.error.set('Failed to load report card.');
        }
        this.loading.set(false);
      }
    });
  }

  printReport() {
    window.print();
  }
}
