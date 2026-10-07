import { inject, Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { API_URL } from '../tokens/api-url.token';
import { AuthResponse, AuthState, LoginRequest, RegisterRequest, UserRole } from '../models/auth.models';

const STORAGE_KEY = 'la_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = inject(API_URL);

  private readonly _state = signal<AuthState | null>(this.loadFromStorage());

  readonly user = computed(() => this._state());
  readonly isLoggedIn = computed(() => this._state() !== null);
  readonly role = computed(() => this._state()?.role ?? null);
  readonly nombreCompleto = computed(() => this._state()?.nombreCompleto ?? '');

  isRole(...roles: UserRole[]): boolean {
    const r = this.role();
    return r !== null && roles.includes(r);
  }

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/login`, req).pipe(
      tap(r => this.setAuth(r))
    );
  }

  register(req: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/register`, req).pipe(
      tap(r => this.setAuth(r))
    );
  }

  refresh(): Observable<AuthResponse> {
    const rt = this._state()?.refreshToken;
    return this.http.post<AuthResponse>(`${this.apiUrl}/api/auth/refresh`, { refreshToken: rt }).pipe(
      tap(r => this.setAuth(r))
    );
  }

  logout(): void {
    const rt = this._state()?.refreshToken;
    if (rt) {
      this.http.post(`${this.apiUrl}/api/auth/logout`, { refreshToken: rt }).subscribe();
    }
    this.clearAuth();
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return this._state()?.accessToken ?? null;
  }

  private setAuth(response: AuthResponse): void {
    const state: AuthState = {
      accessToken:   response.accessToken,
      refreshToken:  response.refreshToken,
      expiresAt:     response.expiresAt,
      nombreCompleto: response.nombreCompleto,
      email:         response.email,
      role:          response.role as UserRole
    };
    this._state.set(state);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }

  private clearAuth(): void {
    this._state.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private loadFromStorage(): AuthState | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const state = JSON.parse(raw) as AuthState;
      if (new Date(state.expiresAt) < new Date()) return null;
      return state;
    } catch {
      return null;
    }
  }

  redirectAfterLogin(): void {
    const role = this.role();
    if (role === 'Admin') this.router.navigate(['/admin']);
    else if (role === 'Vendedor') this.router.navigate(['/vendedor']);
    else this.router.navigate(['/tienda']);
  }
}
