import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminNavComponent } from '../../../shared/components/admin-nav/admin-nav.component';
import { AdminService } from '../services/admin.service';

@Component({
  selector: 'app-roll-number-settings',
  standalone: true,
  imports: [FormsModule, AdminNavComponent],
  templateUrl: './roll-number-settings.component.html',
  styleUrl: './roll-number-settings.component.scss'
})
export class RollNumberSettingsComponent implements OnInit {
  private adminService = inject(AdminService);

  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  saved = signal(false);

  prefix = signal('');
  sequenceDigits = signal(3);

  // Live example so the Admin can see what the format will actually look
  // like before saving — same prefix/padding, a stand-in class code and
  // admission year, and sequence 1.
  examplePreview = computed(() => {
    const digits = Math.max(1, Math.min(6, this.sequenceDigits() || 1));
    const paddedSequence = '1'.padStart(digits, '0');
    return `${this.prefix() || 'R'}24PGA${paddedSequence}`;
  });

  ngOnInit() {
    this.loading.set(true);
    this.adminService.getRollNumberSettings().subscribe({
      next: (s) => {
        this.prefix.set(s.prefix);
        this.sequenceDigits.set(s.sequenceDigits);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load roll number settings.');
        this.loading.set(false);
      }
    });
  }

  save() {
    const prefix = this.prefix().trim();
    if (!prefix) {
      this.error.set('Prefix cannot be blank.');
      return;
    }

    this.error.set(null);
    this.saved.set(false);
    this.saving.set(true);
    this.adminService.updateRollNumberSettings({ prefix, sequenceDigits: this.sequenceDigits() }).subscribe({
      next: () => { this.saving.set(false); this.saved.set(true); },
      error: (err) => {
        this.error.set(err?.error?.message ?? 'Could not save roll number settings.');
        this.saving.set(false);
      }
    });
  }
}
