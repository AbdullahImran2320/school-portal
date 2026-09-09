import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { FeesService } from '../services/fees.service';
import { CollectionSummary } from '../models/fee.models';
import { FeesNavComponent } from '../../../shared/components/fees-nav/fees-nav.component';
import { downloadBlob } from '../../../shared/utils/download-file';

const MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];

@Component({
  selector: 'app-collection-summary',
  standalone: true,
  imports: [FormsModule, FeesNavComponent, DecimalPipe],
  templateUrl: './collection-summary.component.html',
  styleUrl: './collection-summary.component.scss'
})
export class CollectionSummaryComponent implements OnInit {
  private feesService = inject(FeesService);

  monthNames = MONTH_NAMES;
  selectedMonth = signal(new Date().getMonth() + 1);
  selectedYear = signal(new Date().getFullYear());

  summary = signal<CollectionSummary | null>(null);
  loading = signal(false);
  error = signal<string | null>(null);
  exportError = signal<string | null>(null);
  exporting = signal(false);

  yearOptions = computed(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1];
  });

  ngOnInit() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.exportError.set(null);
    this.feesService.getCollectionSummary(this.selectedMonth(), this.selectedYear()).subscribe({
      next: (data) => { this.summary.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load the collection summary.'); this.loading.set(false); }
    });
  }

  exportToExcel() {
    this.exportError.set(null);
    this.exporting.set(true);
    this.feesService.exportCollectionSummary(this.selectedMonth(), this.selectedYear()).subscribe({
      next: (blob) => {
        const monthTag = `${this.selectedYear()}-${String(this.selectedMonth()).padStart(2, '0')}`;
        downloadBlob(blob, `Collection-Summary-${monthTag}.xlsx`);
        this.exporting.set(false);
      },
      error: () => {
        this.exportError.set('Could not export the collection summary.');
        this.exporting.set(false);
      }
    });
  }
}
