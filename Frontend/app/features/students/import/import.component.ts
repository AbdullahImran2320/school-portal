import { Component, inject, signal } from '@angular/core';
import { StudentImportApiService } from './import.service';
import { StudentImportResult } from './import.models';
import { downloadBlob } from '../../../shared/utils/download-file';

@Component({
  selector: 'app-student-import',
  standalone: true,
  imports: [],
  templateUrl: './import.component.html',
  styleUrl: './import.component.scss'
})
export class StudentImportComponent {
  private importService = inject(StudentImportApiService);

  selectedFile = signal<File | null>(null);
  importing = signal(false);
  downloadingTemplate = signal(false);
  error = signal<string | null>(null);
  result = signal<StudentImportResult | null>(null);

  downloadTemplate() {
    this.downloadingTemplate.set(true);
    this.importService.downloadTemplate().subscribe({
      next: (blob) => {
        downloadBlob(blob, 'Student-Import-Template.xlsx');
        this.downloadingTemplate.set(false);
      },
      error: () => {
        this.error.set('Could not download the template.');
        this.downloadingTemplate.set(false);
      }
    });
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
    this.result.set(null);
    this.error.set(null);
  }

  runImport() {
    const file = this.selectedFile();
    if (!file) return;

    this.importing.set(true);
    this.error.set(null);
    this.result.set(null);

    this.importService.import(file).subscribe({
      next: (data) => {
        this.result.set(data);
        this.importing.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Import failed. Check the file and try again.');
        this.importing.set(false);
      }
    });
  }
}
