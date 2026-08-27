import { Component, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { StudentsService } from '../services/students.service';
import { StudentDto } from '../models/student.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './student-list.component.html',
  styleUrl: './student-list.component.scss'
})
export class StudentListComponent implements OnInit {
  students = signal<StudentDto[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  deleteError = signal<string | null>(null);
  searchTerm = signal('');

  filteredStudents = computed(() => {
    const term = this.searchTerm().toLowerCase().trim();
    if (!term) return this.students();
    return this.students().filter(s =>
      s.name.toLowerCase().includes(term) ||
      s.bFormNumber.toLowerCase().includes(term) ||
      s.className.toLowerCase().includes(term)
    );
  });

  constructor(private studentsService: StudentsService, public auth: AuthService) {}

  ngOnInit() {
    this.loadStudents();
  }

  loadStudents() {
    this.loading.set(true);
    this.error.set(null);
    this.studentsService.getAll().subscribe({
      next: (data) => {
        this.students.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load students. Check that the backend is running.');
        this.loading.set(false);
      }
    });
  }

  isAdmin() {
    return this.auth.role() === 'Admin';
  }

  deleteStudent(student: StudentDto) {
    this.deleteError.set(null);
    if (!confirm(`Delete ${student.name}? This cannot be undone.`)) return;

    this.studentsService.delete(student.studentId).subscribe({
      next: () => {
        this.students.update(list => list.filter(s => s.studentId !== student.studentId));
      },
      error: () => {
        this.deleteError.set(`Can't delete ${student.name} — they likely have existing payment records. Consider marking them Withdrawn instead.`);
      }
    });
  }
}