import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { StudentsService } from '../services/students.service';
import { StudentProfile } from './profile.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-student-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, DecimalPipe, DatePipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class StudentProfileComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private studentsService = inject(StudentsService);
  auth = inject(AuthService);

  profile = signal<StudentProfile | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  photoUrl = signal<string | null>(null);
  photoError = signal<string | null>(null);
  uploadingPhoto = signal(false);

  isAdmin() {
    return this.auth.role() === 'Admin';
  }

  ngOnInit() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.error.set('No student specified.');
      this.loading.set(false);
      return;
    }

    this.studentsService.getProfile(id).subscribe({
      next: (data) => {
        this.profile.set(data);
        this.loading.set(false);
        if (data.hasPhoto) this.loadPhoto(id);
      },
      error: () => { this.error.set('Could not load this student. Check that the backend is running.'); this.loading.set(false); }
    });
  }

  private loadPhoto(id: number) {
    this.studentsService.getPhoto(id).subscribe({
      next: (blob) => this.photoUrl.set(URL.createObjectURL(blob)),
      error: () => {} // a missing/broken photo just falls back to the placeholder silently
    });
  }

  onPhotoSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const id = this.profile()?.studentId;
    if (!file || !id) return;

    this.photoError.set(null);
    this.uploadingPhoto.set(true);
    this.studentsService.uploadPhoto(id, file).subscribe({
      next: () => {
        this.uploadingPhoto.set(false);
        this.loadPhoto(id);
      },
      error: (err) => {
        this.uploadingPhoto.set(false);
        this.photoError.set(err?.error?.message ?? 'Could not upload photo.');
      }
    });
    input.value = ''; // lets the same file be re-selected later if needed
  }

  removePhoto() {
    const id = this.profile()?.studentId;
    if (!id) return;
    if (!confirm('Remove this photo?')) return;

    this.studentsService.deletePhoto(id).subscribe({
      next: () => this.photoUrl.set(null),
      error: () => this.photoError.set('Could not remove photo.')
    });
  }

  telHref(mobile: string): string {
    return `tel:${mobile.replace(/[^\d+]/g, '')}`;
  }
}
