import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar">
      <a class="brand" routerLink="/tienda/catalogo">📚 CatalogSync</a>

      <div class="links">
        @if (auth.isRole('Admin')) {
          <a routerLink="/admin/libros" routerLinkActive="active">Libros</a>
          <a routerLink="/admin/catalogo" routerLinkActive="active">Excel</a>
          <a routerLink="/admin/usuarios" routerLinkActive="active">Usuarios</a>
          <a routerLink="/vendedor/busqueda" routerLinkActive="active">Buscar</a>
        }
        @if (auth.isRole('Vendedor')) {
          <a routerLink="/vendedor/busqueda" routerLinkActive="active">Buscar</a>
        }
        @if (auth.isLoggedIn()) {
          <a routerLink="/tienda/catalogo" routerLinkActive="active">Catálogo</a>
          <a routerLink="/tienda/favoritos" routerLinkActive="active">Favoritos</a>
        } @else {
          <a routerLink="/tienda/catalogo" routerLinkActive="active">Catálogo</a>
        }
      </div>

      <div class="user">
        @if (auth.isLoggedIn()) {
          <span class="nombre">{{ auth.nombreCompleto() }}</span>
          <span class="role-badge">{{ auth.role() }}</span>
          <button class="btn-logout" (click)="auth.logout()">Salir</button>
        } @else {
          <a class="btn-login" routerLink="/login">Iniciar sesión</a>
        }
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      display: flex; align-items: center; gap: 1.5rem;
      padding: .75rem 1.5rem; background: #1a1a2e; color: white;
    }
    .brand { font-weight: 700; font-size: 1.1rem; margin-right: auto; }
    .links { display: flex; gap: 1rem; }
    .links a { color: rgba(255,255,255,.75); text-decoration: none; font-size: .9rem; padding: .25rem .5rem; border-radius: 4px; }
    .links a.active, .links a:hover { color: white; background: rgba(255,255,255,.1); }
    .user { display: flex; align-items: center; gap: .75rem; }
    .nombre { font-size: .9rem; }
    .role-badge {
      font-size: .75rem; padding: .15rem .5rem; border-radius: 20px;
      background: #5865f2; color: white;
    }
    .btn-logout {
      padding: .3rem .75rem; background: transparent; border: 1px solid rgba(255,255,255,.4);
      color: white; border-radius: 4px; cursor: pointer; font-size: .85rem;
    }
    .btn-logout:hover { background: rgba(255,255,255,.1); }
    .btn-login {
      padding: .3rem .85rem; background: #5865f2; color: white !important;
      border-radius: 4px; font-size: .85rem; text-decoration: none;
    }
    .btn-login:hover { background: #4752c4; }
    .brand { text-decoration: none; color: white; }
  `]
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
}
