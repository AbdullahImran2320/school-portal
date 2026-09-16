import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { StudentsService } from '../services/students.service';
import { StudentProfile } from '../profile/profile.models';

@Component({
  selector: 'app-student-id-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './id-card.component.html',
  styleUrl: './id-card.component.scss'
})
export class StudentIdCardComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private studentsService = inject(StudentsService);

  profile = signal<StudentProfile | null>(null);
  photoUrl = signal<string | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

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
        if (data.hasPhoto) {
          this.studentsService.getPhoto(id).subscribe({
            next: (blob) => this.photoUrl.set(URL.createObjectURL(blob)),
            error: () => {}
          });
        }
      },
      error: () => { this.error.set('Could not load this student.'); this.loading.set(false); }
    });
  }

  print() { window.print(); }
}
