import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { tap } from 'rxjs/operators';
import { LoginRequest, RegisterRequest, LoginResult, RegisterResult } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private storageKey = 'school-portal-auth';
  currentUser = signal<LoginResult | null>(this.loadFromStorage());
  isLoggedIn = computed(() => this.currentUser() !== null);
  role = computed(() => this.currentUser()?.role ?? null);

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: LoginRequest) {
  return this.http.post<LoginResult>(`${environment.apiUrl}/auth/login`, credentials)
    .pipe(tap(result => this.setSession(result)));
}
  

  register(data: RegisterRequest) {
    return this.http.post<RegisterResult>(`${environment.apiUrl}/auth/register`, data);
  }

  private loadFromStorage(): LoginResult | null {
    const raw = localStorage.getItem(this.storageKey);
    return raw ? JSON.parse(raw) : null;
  }

  setSession(result: LoginResult) {
    localStorage.setItem(this.storageKey, JSON.stringify(result));
    this.currentUser.set(result);
  }

  logout() {
    localStorage.removeItem(this.storageKey);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return this.currentUser()?.token ?? null;
  }
}