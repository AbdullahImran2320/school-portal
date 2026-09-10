import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NgIcon } from '@ng-icons/core';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, NgIcon],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  username = '';
  password = '';
  errorMessage = signal('');
  loading = signal(false);

  ngOnInit() {
    // Set by sessionExpiredInterceptor when a 401 bounces the user here
    // mid-session, so it reads as "please log in again" rather than looking
    // like the login page just rejected a login they never attempted.
    if (this.route.snapshot.queryParamMap.get('sessionExpired')) {
      this.errorMessage.set('Your session expired. Please log in again.');
    }
  }

  submit() {
    if (!this.username || !this.password) {
      this.errorMessage.set('Enter both a username and password.');
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService.login({ username: this.username, password: this.password }).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err) => {
        this.loading.set(false);
        if (err.status === 401) {
          this.errorMessage.set('Invalid username or password.');
        } else if (err.status === 429) {
          // Server message already includes the exact minutes remaining
          this.errorMessage.set(err.error?.message ?? 'Too many failed attempts. Try again later.');
        } else {
          this.errorMessage.set('Something went wrong. Try again.');
        }
      }
    });
  }
}