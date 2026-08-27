import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NgIcon } from '@ng-icons/core';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, NgIcon],
  templateUrl: './register.component.html',
  styleUrl: '../login/login.component.scss' // reuse the same auth-page styling
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  username = '';
  password = '';
  fullName = '';
  errorMessage = signal('');
  successMessage = signal('');
  loading = signal(false);

  submit() {
    if (!this.username || !this.password || !this.fullName) {
      this.errorMessage.set('Fill in every field.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.register({ username: this.username, password: this.password, fullName: this.fullName }).subscribe({
      next: () => {
        this.successMessage.set('Account created. An admin needs to approve your access before you can sign in.');
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        // 409 with errorCode "UsernameTaken" — the specific case the backend
        // was built to signal, so offer the Login form instead of a dead end.
        if (err.status === 409 && err.error?.errorCode === 'UsernameTaken') {
          this.errorMessage.set('That username is already taken. Try logging in instead.');
        } else {
          this.errorMessage.set('Something went wrong. Try again.');
        }
      }
    });
  }
}