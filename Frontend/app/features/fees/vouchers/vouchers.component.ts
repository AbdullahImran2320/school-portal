import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClassesService } from '../../students/services/classes.service';
import { VouchersService } from '../services/vouchers.service';
import { ClassDto } from '../../students/models/class.models';
import { FeeVoucher } from '../models/fee.models';
import { FeesNavComponent } from '../../../shared/components/fees-nav/fees-nav.component';
import { DatePipe, DecimalPipe } from '@angular/common';

const MONTH_NAMES = ['January','February','March','April','May','June','July','August','September','October','November','December'];

@Component({
  selector: 'app-vouchers',
  standalone: true,
  imports: [FormsModule, FeesNavComponent, DecimalPipe, DatePipe],
  templateUrl: './vouchers.component.html',
  styleUrl: './vouchers.component.scss'
})
export class VouchersComponent implements OnInit {
  private classesService = inject(ClassesService);
  private vouchersService = inject(VouchersService);

  monthNames = MONTH_NAMES;
  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  selectedMonth = signal(new Date().getMonth() + 1);
  selectedYear = signal(new Date().getFullYear());
  vouchers = signal<FeeVoucher[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  yearOptions = computed(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1];
  });

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => { this.classes.set(data); if (data.length > 0) this.selectedClassId.set(data[0].classId); },
      error: () => this.error.set('Could not load classes.')
    });
  }

  loadVouchers() {
    const classId = this.selectedClassId();
    if (!classId) return;
    this.loading.set(true);
    this.error.set(null);
    this.vouchersService.getClassVouchers(classId, this.selectedMonth(), this.selectedYear()).subscribe({
      next: (data) => { this.vouchers.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load vouchers for this class/month.'); this.loading.set(false); }
    });
  }

  printAll() { window.print(); }
}