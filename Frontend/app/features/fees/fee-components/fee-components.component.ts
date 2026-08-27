import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ClassesService } from '../../students/services/classes.service';
import { FeeComponentsService } from '../services/fee-components.service';
import { AuthService } from '../../../core/services/auth.service';
import { ClassDto } from '../../students/models/class.models';
import { FeeComponent, FeeFrequency, UpsertFeeComponent } from '../models/fee.models';
import { FeesNavComponent } from '../../../shared/components/fees-nav/fees-nav.component';
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-fee-components',
  standalone: true,
  imports: [FormsModule, FeesNavComponent, DecimalPipe],
  templateUrl: './fee-components.component.html',
  styleUrl: './fee-components.component.scss'
})
export class FeeComponentsComponent implements OnInit {
  private classesService = inject(ClassesService);
  private feeComponentsService = inject(FeeComponentsService);
  private authService = inject(AuthService);

  classes = signal<ClassDto[]>([]);
  selectedClassId = signal<number | null>(null);
  components = signal<FeeComponent[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  isAdmin = computed(() => this.authService.role() === 'Admin');

  editingId = signal<number | null>(null);
  editAmount = signal<number>(0);
  editFrequency = signal<FeeFrequency>('Monthly');

  addingNew = signal(false);
  newName = signal('');
  newAmount = signal<number>(0);
  newFrequency = signal<FeeFrequency>('Monthly');
  newYear = signal('2026');

  rolloverFrom = signal('2026');
  rolloverTo = signal('2027');
  rolloverMessage = signal<string | null>(null);

  frequencies: FeeFrequency[] = ['OneTime', 'Yearly', 'Monthly'];

  ngOnInit() {
    this.classesService.getAll().subscribe({
      next: (data) => {
        this.classes.set(data);
        if (data.length > 0) { this.selectedClassId.set(data[0].classId); this.loadComponents(); }
      },
      error: () => this.error.set('Could not load classes.')
    });
  }

  onClassChange() { this.loadComponents(); }

  loadComponents() {
    const classId = this.selectedClassId();
    if (!classId) return;
    this.loading.set(true);
    this.error.set(null);
    this.feeComponentsService.getByClass(classId).subscribe({
      next: (data) => { this.components.set(data); this.loading.set(false); },
      error: () => { this.error.set('Could not load fee components for this class.'); this.loading.set(false); }
    });
  }

  startEdit(component: FeeComponent) {
    this.editingId.set(component.feeComponentId);
    this.editAmount.set(component.amount);
    this.editFrequency.set(component.frequency);
  }

  cancelEdit() { this.editingId.set(null); }

  saveEdit(component: FeeComponent) {
    const dto: UpsertFeeComponent = {
      componentName: component.componentName,
      amount: this.editAmount(),
      frequency: this.editFrequency(),
      academicYear: component.academicYear,
      classId: component.classId
    };
    this.feeComponentsService.update(component.feeComponentId, dto).subscribe({
      next: () => { this.editingId.set(null); this.loadComponents(); },
      error: () => this.error.set('Could not save the change.')
    });
  }

  deleteComponent(component: FeeComponent) {
    if (!confirm(`Remove "${component.componentName}" from ${component.className}?`)) return;
    this.feeComponentsService.delete(component.feeComponentId).subscribe({
      next: () => this.loadComponents(),
      error: () => this.error.set('Could not delete this component.')
    });
  }

  submitNew() {
    const classId = this.selectedClassId();
    if (!classId || !this.newName().trim() || this.newAmount() <= 0) {
      this.error.set('Fill in a name and an amount greater than 0.');
      return;
    }
    const dto: UpsertFeeComponent = {
      componentName: this.newName(), amount: this.newAmount(), frequency: this.newFrequency(),
      academicYear: this.newYear(), classId
    };
    this.feeComponentsService.create(dto).subscribe({
      next: () => { this.addingNew.set(false); this.newName.set(''); this.newAmount.set(0); this.loadComponents(); },
      error: () => this.error.set('Could not add the new component.')
    });
  }

  submitRollover() {
    this.rolloverMessage.set(null);
    this.feeComponentsService.rollover(this.rolloverFrom(), this.rolloverTo()).subscribe({
      next: (res) => { this.rolloverMessage.set(res.message); this.loadComponents(); },
      error: (err) => this.rolloverMessage.set(err?.error?.message ?? 'Rollover failed.')
    });
  }
}