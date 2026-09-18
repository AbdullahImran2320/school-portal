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
      s.className.toLowerCase().includes(term) ||
      (s.rollNumber != null && s.rollNumber.toString().includes(term))
    );
  });

  missingRollNumberCount = computed(() =>
    this.students().filter(s => s.rollNumber == null && s.admissionStatus === 'Admitted').length
  );

  assigningRollNumbers = signal(false);
  assignRollNumbersMessage = signal<string | null>(null);

  savingRollNumberFor = signal<number | null>(null);
  rollNumberError = signal<string | null>(null);

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

  assignRollNumbers() {
    this.assigningRollNumbers.set(true);
    this.assignRollNumbersMessage.set(null);
    this.studentsService.assignMissingRollNumbers().subscribe({
      next: (result) => {
        this.assigningRollNumbers.set(false);
        const parts: string[] = [];
        parts.push(
          result.assignedCount > 0
            ? `Assigned roll numbers to ${result.assignedCount} student(s).`
            : 'Everyone already has a roll number.'
        );
        if (result.skippedNoClassCodeCount > 0) {
          parts.push(`${result.skippedNoClassCodeCount} student(s) skipped — their class has no roll number code set yet.`);
        }
        this.assignRollNumbersMessage.set(parts.join(' '));
        this.loadStudents(); // refresh so the new numbers show in the table immediately
      },
      error: () => {
        this.assigningRollNumbers.set(false);
        this.assignRollNumbersMessage.set('Could not assign roll numbers. Try again.');
      }
    });
  }

  // Edited directly in the table (a number input per row, admin only) —
  // the student's position within their own class/section, not the
  // formatted code itself. Saving reorders every classmate who already
  // has a position — moving this one to a new spot shifts everyone
  // between the old and new position by one, the same as dragging an
  // item to a new position in an ordered list — so the WHOLE table is
  // reloaded after saving, not just this one row, since other rows'
  // positions (and formatted codes) likely changed too.
  onRollNumberChanged(student: StudentDto, rawValue: string) {
    const value = Number(rawValue);
    if (!rawValue || !Number.isInteger(value) || value < 1) {
      this.rollNumberError.set('Roll number position must be a whole number greater than 0.');
      this.loadStudents(); // snap the input back to the real stored value
      return;
    }
    if (value === student.rollNumberSequence) return; // unchanged, nothing to save

    this.rollNumberError.set(null);
    this.savingRollNumberFor.set(student.studentId);
    this.studentsService.setRollNumber(student.studentId, value).subscribe({
      next: () => {
        this.savingRollNumberFor.set(null);
        this.loadStudents();
      },
      error: (err) => {
        this.savingRollNumberFor.set(null);
        this.rollNumberError.set(err?.error?.message ?? 'Could not update roll number.');
        this.loadStudents(); // snap back to the real value on failure too
      }
    });
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