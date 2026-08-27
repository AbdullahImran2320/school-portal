import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { AcademicsNavComponent } from '../../../shared/components/academics-nav/academics-nav.component';
import { ClassesService } from '../../students/services/classes.service';
import { AcademicsService } from '../services/academics.service';
import { ClassDto } from '../../students/models/class.models';
import { SubjectDto } from '../models/academics.models';

@Component({
  selector: 'app-subjects',
  standalone: true,
  imports: [CommonModule, FormsModule, AcademicsNavComponent],
  templateUrl: './subjects.component.html',
  styleUrl: './subjects.component.scss'
})
export class SubjectsComponent implements OnInit {
  private classesService = inject(ClassesService);
  private academicsService = inject(AcademicsService);

  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  
  subjects = signal<SubjectDto[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // Add Subject Form
  newSubjectName = signal('');
  adding = signal(false);

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => {
        this.classes.set(data);
        if (data.length > 0) {
          this.selectedClassId.set(data[0].classId);
          this.loadSubjects();
        }
      },
      error: () => this.error.set('Could not load classes.')
    });
  }

  onClassChange() {
    this.loadSubjects();
  }

  loadSubjects() {
    const classId = this.selectedClassId();
    if (!classId) return;

    this.loading.set(true);
    this.error.set(null);
    this.academicsService.getSubjectsByClass(classId).subscribe({
      next: (data) => {
        this.subjects.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load subjects.');
        this.loading.set(false);
      }
    });
  }

  addSubject() {
    const classId = this.selectedClassId();
    const name = this.newSubjectName().trim();
    if (!classId || !name) return;

    this.adding.set(true);
    this.academicsService.createSubject({ subjectName: name, classId }).subscribe({
      next: (newSub) => {
        this.subjects.update(subs => [...subs, newSub]);
        this.newSubjectName.set('');
        this.adding.set(false);
      },
      error: () => {
        this.error.set('Failed to add subject. Name must be at least 2 characters.');
        this.adding.set(false);
      }
    });
  }

  deleteSubject(id: number) {
    if (!confirm('Are you sure you want to delete this subject?')) return;
    
    this.academicsService.deleteSubject(id).subscribe({
      next: () => {
        this.subjects.update(subs => subs.filter(s => s.subjectId !== id));
      },
      error: () => {
        this.error.set('Failed to delete subject.');
      }
    });
  }
}
