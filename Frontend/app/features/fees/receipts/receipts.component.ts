import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClassesService } from '../../students/services/classes.service';
import { VouchersService } from '../services/vouchers.service';
import { ClassDto } from '../../students/models/class.models';
import { PaidReceipt } from '../models/fee.models';
import { FeesNavComponent } from '../../../shared/components/fees-nav/fees-nav.component';
import { DatePipe, DecimalPipe } from '@angular/common';

const MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];

@Component({
  selector: 'app-receipts',
  standalone: true,
  imports: [FormsModule, FeesNavComponent, DecimalPipe, DatePipe],
  templateUrl: './receipts.component.html',
  styleUrl: './receipts.component.scss'
})
export class ReceiptsComponent implements OnInit {
  private classesService = inject(ClassesService);
  private vouchersService = inject(VouchersService);

  monthNames = MONTH_NAMES;
  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedMonth = signal(new Date().getMonth() + 1);
  selectedYear = signal(new Date().getFullYear());
  receipts = signal<PaidReceipt[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  // When set to a receipt number, only that one card renders during print;
  // null means "print all" (the default view).
  printingOnly = signal<string | null>(null);

  yearOptions = computed(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1];
  });

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => { this.classes.set(data); if (data.length > 0) this.selectedClassId.set(data[0].classId); },
      error: () => this.error.set('Could not load classes.')
    });

    // Reset back to showing every receipt once the print dialog closes,
    // whether the user printed or cancelled.
    window.addEventListener('afterprint', () => this.printingOnly.set(null));
  }

  loadReceipts() {
    const classId = this.selectedClassId();
    if (!classId) return;
    this.loading.set(true);
    this.error.set(null);
    this.vouchersService.getClassReceipts(classId, this.selectedMonth(), this.selectedYear()).subscribe({
      next: (data) => { this.receipts.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load receipts for this class/month.'); this.loading.set(false); }
    });
  }

  printAll() {
    this.printingOnly.set(null);
    window.print();
  }

  printOne(receiptNumber: string) {
    this.printingOnly.set(receiptNumber);
    setTimeout(() => window.print());
  }
}