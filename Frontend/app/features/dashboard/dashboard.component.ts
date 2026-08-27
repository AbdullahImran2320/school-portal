import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon } from '@ng-icons/core';
import { DashboardService } from './services/dashboard.service';
import { DashboardSummary } from './models/dashboard.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, NgIcon],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);

  summary = signal<DashboardSummary | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);

    this.dashboardService.getSummary().subscribe({
      next: data => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load dashboard data. Please try again.');
        this.loading.set(false);
      }
    });
  }

  formatCurrency(amount: number): string {
    return `Rs ${Math.round(amount).toLocaleString('en-PK')}`;
  }

  formatAttendanceDate(value: string): string {
    const date = new Date(value);
    return date.toLocaleDateString('en-PK', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  trackClass(_: number, row: { classId: number }): number {
    return row.classId;
  }
}
