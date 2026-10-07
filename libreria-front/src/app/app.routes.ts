import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard('Admin')],
    loadComponent: () =>
      import('./features/admin/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      {
        path: 'catalogo',
        loadComponent: () =>
          import('./features/catalogo/catalogo.component').then(m => m.CatalogoComponent)
      },
      {
        path: 'libros',
        loadComponent: () =>
          import('./features/admin/libros/libros.component').then(m => m.LibrosComponent)
      },
      {
        path: 'usuarios',
        loadComponent: () =>
          import('./features/admin/usuarios/usuarios.component').then(m => m.UsuariosComponent)
      },
      { path: '', redirectTo: 'libros', pathMatch: 'full' }
    ]
  },
  {
    path: 'vendedor',
    canActivate: [authGuard, roleGuard('Admin', 'Vendedor')],
    loadComponent: () =>
      import('./features/vendedor/vendedor-layout/vendedor-layout.component').then(m => m.VendedorLayoutComponent),
    children: [
      {
        path: 'busqueda',
        loadComponent: () =>
          import('./features/vendedor/busqueda/busqueda.component').then(m => m.BusquedaComponent)
      },
      { path: '', redirectTo: 'busqueda', pathMatch: 'full' }
    ]
  },
  {
    path: 'tienda',
    loadComponent: () =>
      import('./features/tienda/tienda-layout/tienda-layout.component').then(m => m.TiendaLayoutComponent),
    children: [
      {
        path: 'catalogo',
        loadComponent: () =>
          import('./features/tienda/catalogo-tienda/catalogo-tienda.component').then(m => m.CatalogoTiendaComponent)
      },
      {
        path: 'favoritos',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./features/tienda/favoritos/favoritos.component').then(m => m.FavoritosComponent)
      },
      { path: '', redirectTo: 'catalogo', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'login' }
];
