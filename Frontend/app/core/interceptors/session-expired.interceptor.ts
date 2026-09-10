import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// A 401 from any endpoint other than login/register/change-password means
// the token that was attached is no longer valid — expired, or the user was
// deleted/deactivated server-side. Without this, every screen just shows
// its own generic "could not load" message with no indication of why, and
// the person has no way to know they need to log in again versus the
// portal being broken.
//
// Login/register/change-password are excluded because a 401 from THOSE is
// a normal, expected user-facing outcome ("wrong password") that belongs on
// that same page, not a reason to redirect away from it.
const EXCLUDED_PATHS = ['/auth/login', '/auth/register', '/auth/change-password'];

export const sessionExpiredInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse
        && error.status === 401
        && !EXCLUDED_PATHS.some(path => req.url.includes(path))) {
        authService.logout();
        router.navigate(['/login'], { queryParams: { sessionExpired: '1' } });
      }
      return throwError(() => error);
    })
  );
};
