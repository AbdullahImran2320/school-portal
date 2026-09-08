import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './change-password.component.html',
  styleUrl: './change-password.component.scss'
})
export class ChangePasswordComponent {
  private authService = inject(AuthService);

  currentPassword = signal('');
  newPassword = signal('');
  confirmPassword = signal('');

  error = signal<string | null>(null);
  success = signal(false);
  saving = signal(false);

  submit() {
    this.error.set(null);
    this.success.set(false);

    if (!this.currentPassword() || !this.newPassword() || !this.confirmPassword()) {
      this.error.set('Fill in all three fields.');
      return;
    }
    if (this.newPassword().length < 6) {
      this.error.set('New password must be at least 6 characters.');
      return;
    }
    if (this.newPassword() !== this.confirmPassword()) {
      this.error.set("New password and confirmation don't match.");
      return;
    }

    this.saving.set(true);
    this.authService.changePassword({
      currentPassword: this.currentPassword(),
      newPassword: this.newPassword()
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.success.set(true);
        this.currentPassword.set('');
        this.newPassword.set('');
        this.confirmPassword.set('');
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.message ?? 'Could not change password. Try again.');
      }
    });
  }
}
