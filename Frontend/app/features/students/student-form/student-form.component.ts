import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StudentsService } from '../services/students.service';
import { ClassesService } from '../services/classes.service';
import { ParentsService } from '../services/parents.service';
import { StudentDto, CreateStudentDto, UpdateStudentDto, AdmissionStatus } from '../models/student.models';
import { ClassDto } from '../models/class.models';
import { ParentDto, UpsertParentDto, PrimaryGuardian } from '../models/parent.models';

@Component({
  selector: 'app-student-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './student-form.component.html',
  styleUrl: './student-form.component.scss'
})
export class StudentFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private studentsService = inject(StudentsService);
  private classesService = inject(ClassesService);
  private parentsService = inject(ParentsService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  studentId = signal<number | null>(null);
  isEditMode = computed(() => this.studentId() !== null);

  classes = signal<ClassDto[]>([]);
  loading = signal(true);
  submitting = signal(false);
  submitError = signal<string | null>(null);

  // Parent linking (create mode only)
  parentMode = signal<'search' | 'new'>('search');
  parentSearchMobile = signal('');
  parentSearchResults = signal<ParentDto[]>([]);
  parentSearchLoading = signal(false);
  parentSearchError = signal<string | null>(null);
  selectedParent = signal<ParentDto | null>(null);

  // Edit mode shows the linked parent read-only (PUT /students/{id} doesn't accept parentId)
  existingParentSummary = signal<{ fatherName: string; fatherMobile: string } | null>(null);

  studentForm = this.fb.group({
    name: ['', Validators.required],
    bFormNumber: ['', [Validators.required, Validators.pattern(/^\d{5}-\d{7}-\d{1}$/)]],
    dateOfBirth: ['', Validators.required],
    gender: ['', Validators.required],
    admissionDate: ['', Validators.required],
    admissionStatus: ['Applied' as AdmissionStatus, Validators.required],
    classId: [null as number | null, Validators.required]
  });

  newParentForm = this.fb.nonNullable.group({
    fatherName: ['', Validators.required],
    fatherMobile: ['', Validators.required],
    fatherOccupation: [''],
    motherName: [''],
    motherMobile: [''],
    primaryGuardian: ['Father' as PrimaryGuardian, Validators.required],
    address: ['', Validators.required]
  });


  ngOnInit() {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.studentId.set(Number(idParam));
    }

    this.classesService.getAll().subscribe({
      next: (data) => this.classes.set(data),
      error: () => this.submitError.set('Could not load the class list — check the /api/classes endpoint.')
    });

    if (this.isEditMode()) {
      this.loadStudentForEdit(this.studentId()!);
    } else {
      this.loading.set(false);
      const today = new Date().toISOString().substring(0, 10);
      this.studentForm.patchValue({ admissionDate: today });
    }
  }

  private loadStudentForEdit(id: number) {
    this.studentsService.getById(id).subscribe({
      next: (student: StudentDto) => {
        this.studentForm.patchValue({
          name: student.name,
          bFormNumber: student.bFormNumber,
          dateOfBirth: student.dateOfBirth.substring(0, 10),
          gender: student.gender,
          admissionDate: student.admissionDate.substring(0, 10),
          admissionStatus: student.admissionStatus,
          classId: student.classId
        });
        this.studentForm.get('admissionDate')?.disable();

        this.existingParentSummary.set({
          fatherName: student.fatherName,
          fatherMobile: student.fatherMobile
        });
        this.loading.set(false);
      },
      error: () => {
        this.submitError.set('Could not load this student. They may have been deleted.');
        this.loading.set(false);
      }
    });
  }

  searchParent() {
    const mobile = this.parentSearchMobile().trim();
    if (!mobile) return;

    this.parentSearchLoading.set(true);
    this.parentSearchError.set(null);
    this.parentSearchResults.set([]);

    this.parentsService.searchByMobile(mobile).subscribe({
      next: (results) => {
        this.parentSearchResults.set(results);
        this.parentSearchLoading.set(false);
        if (results.length === 0) {
          this.parentSearchError.set('No existing parent found with that number. You can add a new one instead.');
        }
      },
      error: () => {
        this.parentSearchError.set('Search failed. Check the number and try again.');
        this.parentSearchLoading.set(false);
      }
    });
  }

  selectParent(parent: ParentDto) { this.selectedParent.set(parent); }
  clearSelectedParent() { this.selectedParent.set(null); }
  switchToNewParent() { this.parentMode.set('new'); this.selectedParent.set(null); }
  switchToSearchParent() { this.parentMode.set('search'); }

  submit() {
    this.submitError.set(null);

    if (this.studentForm.invalid) {
      this.studentForm.markAllAsTouched();
      this.submitError.set('Please fix the highlighted fields.');
      return;
    }

    if (this.isEditMode()) {
      this.submitUpdate();
    } else {
      this.submitCreate();
    }
  }

  private submitCreate() {
    if (this.parentMode() === 'search') {
      const parent = this.selectedParent();
      if (!parent) {
        this.submitError.set('Select an existing parent or switch to "Add new parent" first.');
        return;
      }
      this.createStudentWithParentId(parent.parentId);
    } else {
      if (this.newParentForm.invalid) {
        this.newParentForm.markAllAsTouched();
        this.submitError.set('Please fill in the required parent fields.');
        return;
      }
      this.submitting.set(true);
      const parentDto: UpsertParentDto = this.newParentForm.getRawValue();
      this.parentsService.create(parentDto).subscribe({
        next: (createdParent) => this.createStudentWithParentId(createdParent.parentId),
        error: () => {
          this.submitting.set(false);
          this.submitError.set('Could not save the parent record. Please check the fields and try again.');
        }
      });
    }
  }

  private createStudentWithParentId(parentId: number) {
    this.submitting.set(true);
    const formValue = this.studentForm.getRawValue();
    const dto: CreateStudentDto = {
      name: formValue.name!,
      bFormNumber: formValue.bFormNumber!,
      dateOfBirth: formValue.dateOfBirth!,
      gender: formValue.gender!,
      admissionDate: formValue.admissionDate!,
      admissionStatus: formValue.admissionStatus!,
      classId: formValue.classId!,
      parentId
    };

    this.studentsService.create(dto).subscribe({
      next: () => this.router.navigate(['/students']),
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(this.describeError(err, 'create'));
      }
    });
  }

  private submitUpdate() {
    this.submitting.set(true);
    const formValue = this.studentForm.getRawValue();
    const dto: UpdateStudentDto = {
      name: formValue.name!,
      bFormNumber: formValue.bFormNumber!,
      dateOfBirth: formValue.dateOfBirth!,
      gender: formValue.gender!,
      admissionStatus: formValue.admissionStatus!,
      classId: formValue.classId!
    };

    this.studentsService.update(this.studentId()!, dto).subscribe({
      next: () => this.router.navigate(['/students']),
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(this.describeError(err, 'update'));
      }
    });
  }

  private describeError(err: any, action: 'create' | 'update'): string {
    if (err?.status === 400) return 'Some fields were rejected by the server — double-check the B-Form number format and dates.';
    if (err?.status === 401 || err?.status === 403) return 'You do not have permission to do this.';
    return `Could not ${action} the student. Check that the backend is running and try again.`;
  }
}