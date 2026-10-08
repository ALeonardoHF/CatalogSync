import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { NgClass, DatePipe } from '@angular/common';
import { API_URL } from '../../../core/tokens/api-url.token';

interface UsuarioRow {
  id: string;
  email: string;
  nombreCompleto: string;
  role: string;
  isActive: boolean;
  creadoEn: string;
  ultimoLogin?: string;
}

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [ReactiveFormsModule, NgClass, DatePipe],
  template: `
    <div class="page">
      <div class="page-header">
        <h2>Gestión de Usuarios</h2>
        <button class="btn-primary" (click)="showForm.set(true)">+ Nuevo usuario</button>
      </div>

      <!-- Filtro rol -->
      <div class="filtro-row">
        @for (r of roles; track r.value) {
          <button class="chip" [class.active]="filtroRol() === r.value"
                  (click)="cambiarFiltro(r.value)">
            {{ r.label }}
          </button>
        }
      </div>

      <div class="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>Nombre</th>
              <th>Email</th>
              <th>Rol</th>
              <th>Último login</th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            @for (u of usuarios(); track u.id) {
              <tr [ngClass]="{ inactivo: !u.isActive }">
                <td>{{ u.nombreCompleto }}</td>
                <td class="email">{{ u.email }}</td>
                <td>
                  <span class="role-badge" [ngClass]="'role-' + u.role.toLowerCase()">
                    {{ u.role }}
                  </span>
                </td>
                <td class="muted">{{ u.ultimoLogin ? (u.ultimoLogin | date:'dd/MM/yy HH:mm') : '—' }}</td>
                <td>
                  <span class="status-dot" [ngClass]="u.isActive ? 'ok' : 'off'"></span>
                  {{ u.isActive ? 'Activo' : 'Inactivo' }}
                </td>
                <td class="actions">
                  @if (u.isActive) {
                    <button class="btn-sm danger" (click)="toggleActivo(u)">Desactivar</button>
                  } @else {
                    <button class="btn-sm success" (click)="toggleActivo(u)">Activar</button>
                  }
                </td>
              </tr>
            } @empty {
              <tr><td colspan="6" class="empty">Sin usuarios</td></tr>
            }
          </tbody>
        </table>
      </div>

      <!-- Modal nuevo usuario -->
      @if (showForm()) {
        <div class="modal-backdrop" (click)="cerrar()">
          <div class="modal" (click)="$event.stopPropagation()">
            <div class="modal-head">
              <span class="modal-icon">+</span>
              <div>
                <h3>Nuevo usuario</h3>
                <p class="modal-subtitle">Completar datos de acceso</p>
              </div>
            </div>

            @if (formError()) {
              <div class="alert-error">{{ formError() }}</div>
            }

            <form [formGroup]="form" (ngSubmit)="crear()">
              <div class="field">
                <label>Nombre completo *</label>
                <input formControlName="nombreCompleto" placeholder="Nombre completo" />
              </div>
              <div class="field">
                <label>Email *</label>
                <input formControlName="email" type="email" placeholder="correo@ejemplo.com" />
              </div>
              <div class="field">
                <label>Contraseña *</label>
                <input formControlName="password" type="password" placeholder="Mínimo 8 caracteres" />
              </div>
              <div class="field">
                <label>Rol *</label>
                <select formControlName="role">
                  <option value="Admin">Admin</option>
                  <option value="Vendedor">Vendedor</option>
                  <option value="Cliente">Cliente</option>
                </select>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn-secondary" (click)="cerrar()">Cancelar</button>
                <button type="submit" class="btn-primary" [disabled]="form.invalid || saving()">
                  {{ saving() ? 'Creando...' : 'Crear usuario' }}
                </button>
              </div>
            </form>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .page-header {
      display: flex; justify-content: space-between; align-items: center;
      margin-bottom: 1.25rem;
    }
    h2 { margin: 0; font-size: 1.3rem; color: var(--text); }

    /* Chips filtro */
    .filtro-row { display: flex; gap: .5rem; margin-bottom: 1.25rem; flex-wrap: wrap; }
    .chip {
      all: unset; cursor: pointer;
      padding: .3rem .9rem; border: 1.5px solid var(--border);
      border-radius: 20px; font-size: .83rem; color: var(--muted);
      transition: all .15s;
    }
    .chip:hover { border-color: var(--accent); color: var(--accent); }
    .chip.active { border-color: var(--primary); background: var(--primary); color: #fff; }

    /* Tabla */
    .table-wrapper {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 10px; overflow: hidden; overflow-x: auto;
    }
    .inactivo td { opacity: .4; }
    .email { font-family: monospace; font-size: .83rem; }
    .muted { font-size: .82rem; color: var(--muted); }
    .actions { white-space: nowrap; }

    /* Role badges */
    .role-badge { padding: .18rem .55rem; border-radius: 20px; font-size: .72rem; font-weight: 600; }
    .role-admin    { background: #a855f720; color: var(--accent); border: 1px solid #a855f740; }
    .role-vendedor { background: #3b82f620; color: #60a5fa;       border: 1px solid #3b82f640; }
    .role-cliente  { background: #4ade8020; color: var(--success); border: 1px solid #4ade8040; }

    /* Status dot */
    .status-dot {
      display: inline-block; width: 7px; height: 7px;
      border-radius: 50%; margin-right: .35rem; vertical-align: middle;
    }
    .status-dot.ok  { background: var(--success); }
    .status-dot.off { background: var(--muted); }

    /* Botones tabla */
    .btn-sm {
      all: unset; cursor: pointer;
      padding: .2rem .55rem; font-size: .78rem; border-radius: 5px;
      border: 1px solid var(--border); color: var(--muted);
      transition: border-color .15s, color .15s, background .15s;
    }
    .btn-sm.danger  { border-color: #f8717140; color: var(--error); }
    .btn-sm.danger:hover  { border-color: var(--error); background: #f8717110; }
    .btn-sm.success { border-color: #4ade8040; color: var(--success); }
    .btn-sm.success:hover { border-color: var(--success); background: #4ade8010; }

    .empty { text-align: center; color: var(--muted); padding: 2.5rem; }

    /* Botones header */
    .btn-primary {
      all: unset; cursor: pointer;
      padding: .55rem 1.25rem; background: var(--primary); color: #fff;
      border-radius: 8px; font-size: .9rem; font-weight: 500; transition: opacity .15s;
    }
    .btn-primary:hover:not(:disabled) { opacity: .85; }
    .btn-primary:disabled { opacity: .4; cursor: not-allowed; }
    .btn-secondary {
      all: unset; cursor: pointer;
      padding: .55rem 1.25rem; color: var(--muted);
      border: 1px solid var(--border); border-radius: 8px; font-size: .9rem;
      transition: border-color .15s, color .15s;
    }
    .btn-secondary:hover { border-color: var(--accent); color: var(--accent); }

    /* Modal */
    .modal-backdrop {
      position: fixed; inset: 0; background: rgba(0,0,0,.65);
      display: flex; align-items: center; justify-content: center; z-index: 100;
    }
    .modal {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 14px; padding: 1.5rem; width: 400px; max-width: 95vw;
    }
    .modal-head { display: flex; align-items: flex-start; gap: .85rem; margin-bottom: 1.5rem; }
    .modal-icon {
      flex-shrink: 0; width: 38px; height: 38px; border-radius: 10px;
      background: var(--primary); color: #fff;
      display: flex; align-items: center; justify-content: center;
      font-size: 1.2rem; font-weight: 700;
    }
    .modal-head h3 { margin: 0 0 .2rem; font-size: 1rem; color: var(--text); }
    .modal-subtitle { margin: 0; font-size: .8rem; color: var(--muted); }
    .modal h3 { margin: 0 0 1.25rem; color: var(--text); }
    .field { display: flex; flex-direction: column; gap: .3rem; margin-bottom: .9rem; }
    .field label { font-size: .75rem; font-weight: 600; color: var(--muted); text-transform: uppercase; letter-spacing: .04em; }
    .field input, .field select {
      background: #0a0a18; border: 1px solid var(--border); border-radius: 8px;
      padding: .6rem .75rem; color: var(--text); font-size: .93rem;
      transition: border-color .2s; width: 100%; box-sizing: border-box;
    }
    .field input:focus, .field select:focus { outline: none; border-color: var(--primary); }
    .field select { appearance: none; cursor: pointer;
      background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 24 24' fill='none' stroke='%236b6b8a' stroke-width='2'%3E%3Cpath d='M6 9l6 6 6-6'/%3E%3C/svg%3E");
      background-repeat: no-repeat; background-position: right .75rem center; padding-right: 2.2rem;
    }
    .modal-footer {
      display: flex; justify-content: flex-end; gap: .75rem;
      margin-top: 1.25rem; padding-top: 1rem; border-top: 1px solid var(--border);
    }
    .alert-error {
      background: #f8717110; color: var(--error);
      border: 1px solid #f8717140; border-radius: 6px;
      padding: .5rem .75rem; margin-bottom: .9rem; font-size: .875rem;
    }
  `]
})
export class UsuariosComponent implements OnInit {
  private readonly http   = inject(HttpClient);
  private readonly apiUrl = inject(API_URL);
  private readonly fb     = inject(FormBuilder);

  readonly usuarios  = signal<UsuarioRow[]>([]);
  readonly showForm  = signal(false);
  readonly saving    = signal(false);
  readonly formError = signal('');
  readonly filtroRol = signal('');

  readonly roles = [
    { value: '', label: 'Todos' },
    { value: 'Admin', label: 'Admin' },
    { value: 'Vendedor', label: 'Vendedor' },
    { value: 'Cliente', label: 'Cliente' }
  ];

  readonly form = this.fb.nonNullable.group({
    nombreCompleto: ['', Validators.required],
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role:     ['Vendedor', Validators.required]
  });

  ngOnInit(): void { this.cargar(); }

  cargar(): void {
    const rol = this.filtroRol();
    const url = rol
      ? `${this.apiUrl}/api/usuarios?rol=${rol}`
      : `${this.apiUrl}/api/usuarios`;

    this.http.get<UsuarioRow[]>(url).subscribe({
      next: u => this.usuarios.set(u)
    });
  }

  cambiarFiltro(rol: string): void {
    this.filtroRol.set(rol);
    this.cargar();
  }

  crear(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set('');

    const v = this.form.getRawValue();
    this.http.post(`${this.apiUrl}/api/usuarios`, {
      email: v.email,
      password: v.password,
      nombreCompleto: v.nombreCompleto,
      role: v.role
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.cerrar();
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.formError.set(err.error?.message ?? 'Error al crear usuario.');
      }
    });
  }

  toggleActivo(u: UsuarioRow): void {
    const accion = u.isActive ? 'desactivar' : 'activar';
    this.http.patch(`${this.apiUrl}/api/usuarios/${u.id}/${accion}`, {}).subscribe({
      next: () => this.cargar(),
      error: (err) => alert(err.error?.message ?? 'Error')
    });
  }

  cerrar(): void {
    this.showForm.set(false);
    this.form.reset({ role: 'Vendedor' });
    this.formError.set('');
  }
}
