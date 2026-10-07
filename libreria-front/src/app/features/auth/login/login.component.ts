import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

type Tab = 'login' | 'register';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="page">
      <div class="card">

        <div class="brand">
          <div class="brand-icon">📚</div>
          <div>
            <div class="brand-name">Librería Atenas</div>
            <div class="brand-sub">Sistema de gestión</div>
          </div>
        </div>

        <!-- Tabs -->
        <div class="tabs">
          <button class="tab" [class.active]="tab() === 'login'" (click)="setTab('login')">
            Iniciar sesión
          </button>
          <button class="tab" [class.active]="tab() === 'register'" (click)="setTab('register')">
            Crear cuenta
          </button>
        </div>

        @if (error()) {
          <div class="alert-error">
            <span class="alert-icon">!</span>
            {{ error() }}
          </div>
        }

        @if (tab() === 'login') {

          <form [formGroup]="loginForm" (ngSubmit)="onLogin()">
            <div class="field">
              <label for="email">Correo electrónico</label>
              <div class="input-wrap">
                <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M2.003 5.884L10 9.882l7.997-3.998A2 2 0 0016 4H4a2 2 0 00-1.997 1.884z"/>
                  <path d="M18 8.118l-8 4-8-4V14a2 2 0 002 2h12a2 2 0 002-2V8.118z"/>
                </svg>
                <input id="email" type="email" formControlName="email"
                       placeholder="correo@ejemplo.com" autocomplete="email" />
              </div>
            </div>
            <div class="field">
              <label for="password">Contraseña</label>
              <div class="input-wrap">
                <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/>
                </svg>
                <input id="password" type="password" formControlName="password"
                       placeholder="••••••••" autocomplete="current-password" />
              </div>
            </div>
            <button type="submit" [disabled]="loading() || loginForm.invalid" class="btn-submit">
              @if (loading()) { <span class="spinner"></span> Ingresando... }
              @else { Ingresar }
            </button>
          </form>

        } @else {

          <form [formGroup]="registerForm" (ngSubmit)="onRegister()">
            <div class="field">
              <label for="nombre">Nombre completo</label>
              <div class="input-wrap">
                <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd"/>
                </svg>
                <input id="nombre" type="text" formControlName="nombreCompleto"
                       placeholder="Tu nombre completo" autocomplete="name" />
              </div>
            </div>
            <div class="field">
              <label for="reg-email">Correo electrónico</label>
              <div class="input-wrap">
                <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor">
                  <path d="M2.003 5.884L10 9.882l7.997-3.998A2 2 0 0016 4H4a2 2 0 00-1.997 1.884z"/>
                  <path d="M18 8.118l-8 4-8-4V14a2 2 0 002 2h12a2 2 0 002-2V8.118z"/>
                </svg>
                <input id="reg-email" type="email" formControlName="email"
                       placeholder="correo@ejemplo.com" autocomplete="email" />
              </div>
            </div>
            <div class="field">
              <label for="reg-password">Contraseña</label>
              <div class="input-wrap">
                <svg class="input-icon" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd"/>
                </svg>
                <input id="reg-password" type="password" formControlName="password"
                       placeholder="Mínimo 8 caracteres" autocomplete="new-password" />
              </div>
            </div>
            <button type="submit" [disabled]="loading() || registerForm.invalid" class="btn-submit">
              @if (loading()) { <span class="spinner"></span> Creando cuenta... }
              @else { Crear cuenta }
            </button>
          </form>

        }

        <div class="divider"><span>o</span></div>

        <button class="btn-guest" (click)="entrarComoInvitado()">
          Continuar como invitado →
        </button>

      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
      padding: 1rem;
    }

    .card {
      background: #fff;
      border-radius: 20px;
      padding: 2.25rem 2.25rem 2rem;
      width: 100%;
      max-width: 420px;
      box-shadow: 0 25px 60px rgba(0,0,0,.35);
    }

    .brand {
      display: flex;
      align-items: center;
      gap: .85rem;
      margin-bottom: 1.75rem;
      padding-bottom: 1.5rem;
      border-bottom: 1px solid #f0f0f0;
    }
    .brand-icon {
      font-size: 1.6rem;
      background: linear-gradient(135deg, #1a1a2e, #5865f2);
      border-radius: 12px;
      width: 48px;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
    .brand-name { font-size: 1.15rem; font-weight: 700; color: #1a1a2e; line-height: 1.2; }
    .brand-sub  { font-size: .75rem; color: #aaa; margin-top: .1rem; }

    .tabs {
      display: flex;
      background: #f4f4f6;
      border-radius: 10px;
      padding: 3px;
      margin-bottom: 1.5rem;
      gap: 3px;
    }
    .tab {
      flex: 1;
      padding: .55rem;
      border: none;
      border-radius: 8px;
      font-size: .875rem;
      font-weight: 500;
      cursor: pointer;
      background: transparent;
      color: #888;
      transition: all .2s;
    }
    .tab.active {
      background: #fff;
      color: #1a1a2e;
      font-weight: 600;
      box-shadow: 0 1px 4px rgba(0,0,0,.1);
    }

    .field { margin-bottom: 1rem; }
    label {
      display: block;
      font-size: .78rem;
      font-weight: 600;
      color: #666;
      text-transform: uppercase;
      letter-spacing: .04em;
      margin-bottom: .4rem;
    }
    .input-wrap { position: relative; }
    .input-icon {
      position: absolute;
      left: .85rem;
      top: 50%;
      transform: translateY(-50%);
      width: 15px;
      height: 15px;
      color: #ccc;
      pointer-events: none;
    }
    input {
      width: 100%;
      padding: .72rem .85rem .72rem 2.4rem;
      border: 1.5px solid #e8e8e8;
      border-radius: 10px;
      font-size: .93rem;
      color: #111;
      background: #fafafa;
      outline: none;
      transition: border-color .2s, background .2s, box-shadow .2s;
      box-sizing: border-box;
    }
    input:focus {
      border-color: #5865f2;
      background: #fff;
      box-shadow: 0 0 0 3px rgba(88,101,242,.12);
    }
    input::placeholder { color: #ccc; }

    .btn-submit {
      width: 100%;
      padding: .82rem;
      margin-top: .25rem;
      background: linear-gradient(135deg, #5865f2, #4752c4);
      color: #fff;
      border: none;
      border-radius: 10px;
      font-size: .95rem;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: .55rem;
      transition: opacity .2s, transform .1s;
    }
    .btn-submit:hover:not(:disabled) { opacity: .9; transform: translateY(-1px); }
    .btn-submit:active:not(:disabled) { transform: translateY(0); }
    .btn-submit:disabled { opacity: .55; cursor: not-allowed; }

    .spinner {
      width: 15px;
      height: 15px;
      border: 2px solid rgba(255,255,255,.4);
      border-top-color: #fff;
      border-radius: 50%;
      animation: spin .7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }

    .divider {
      display: flex;
      align-items: center;
      gap: .75rem;
      margin: 1.25rem 0 1rem;
      color: #ddd;
      font-size: .8rem;
    }
    .divider::before, .divider::after {
      content: '';
      flex: 1;
      height: 1px;
      background: #eee;
    }
    .divider span { color: #bbb; }

    .btn-guest {
      width: 100%;
      padding: .7rem;
      background: transparent;
      border: 1.5px solid #e8e8e8;
      border-radius: 10px;
      font-size: .9rem;
      color: #555;
      cursor: pointer;
      transition: border-color .2s, color .2s, background .2s;
    }
    .btn-guest:hover { border-color: #5865f2; color: #5865f2; background: #f7f8ff; }

    .alert-error {
      display: flex;
      align-items: center;
      gap: .6rem;
      background: #fff5f5;
      border: 1px solid #fed7d7;
      color: #c53030;
      border-radius: 8px;
      padding: .65rem .9rem;
      margin-bottom: 1.1rem;
      font-size: .875rem;
    }
    .alert-icon {
      flex-shrink: 0;
      width: 17px;
      height: 17px;
      border-radius: 50%;
      background: #c53030;
      color: white;
      font-size: .65rem;
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  `]
})
export class LoginComponent {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb     = inject(FormBuilder);

  readonly tab     = signal<Tab>('login');
  readonly loading = signal(false);
  readonly error   = signal('');

  readonly loginForm = this.fb.nonNullable.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', Validators.required]
  });

  readonly registerForm = this.fb.nonNullable.group({
    nombreCompleto: ['', Validators.required],
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  setTab(t: Tab): void {
    this.tab.set(t);
    this.error.set('');
  }

  onLogin(): void {
    if (this.loginForm.invalid) return;
    this.loading.set(true);
    this.error.set('');

    const { email, password } = this.loginForm.getRawValue();
    this.auth.login({ email, password }).subscribe({
      next: () => { this.loading.set(false); this.auth.redirectAfterLogin(); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Credenciales incorrectas.');
      }
    });
  }

  onRegister(): void {
    if (this.registerForm.invalid) return;
    this.loading.set(true);
    this.error.set('');

    const { email, password, nombreCompleto } = this.registerForm.getRawValue();
    this.auth.register({ email, password, nombreCompleto }).subscribe({
      next: () => { this.loading.set(false); this.auth.redirectAfterLogin(); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Error al crear la cuenta.');
      }
    });
  }

  entrarComoInvitado(): void {
    this.router.navigate(['/tienda/catalogo']);
  }
}
