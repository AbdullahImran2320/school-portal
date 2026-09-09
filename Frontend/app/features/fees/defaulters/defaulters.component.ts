import { Component, OnInit, inject, signal } from '@angular/core';
import { FeesService } from '../services/fees.service';
import { Defaulter } from '../models/fee.models';
import { FeesNavComponent } from '../../../shared/components/fees-nav/fees-nav.component';
import { DecimalPipe } from '@angular/common';
import { downloadBlob } from '../../../shared/utils/download-file';

@Component({
  selector: 'app-defaulters',
  standalone: true,
  imports: [FeesNavComponent, DecimalPipe],
  templateUrl: './defaulters.component.html',
  styleUrl: './defaulters.component.scss'
})
export class DefaultersComponent implements OnInit {
  private feesService = inject(FeesService);

  defaulters = signal<Defaulter[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  exportError = signal<string | null>(null);
  exporting = signal(false);

  ngOnInit() {
    this.feesService.getDefaulters().subscribe({
      next: (data) => { this.defaulters.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load the defaulters report.'); this.loading.set(false); }
    });
  }

  telHref(mobile: string): string {
    return `tel:${mobile.replace(/[^\d+]/g, '')}`;
  }

  exportToExcel() {
    this.exportError.set(null);
    this.exporting.set(true);
    this.feesService.exportDefaulters().subscribe({
      next: (blob) => {
        downloadBlob(blob, `Defaulters-${new Date().toISOString().slice(0, 10)}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exportError.set('Could not export the defaulters report.');
        this.exporting.set(false);
      }
    });
  }
}