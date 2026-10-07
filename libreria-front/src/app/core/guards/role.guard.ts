import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { UserRole } from '../models/auth.models';

export const roleGuard = (...roles: UserRole[]): CanActivateFn => {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.isRole(...roles)) return true;

    const role = auth.role();
    if (role === 'Admin') return router.createUrlTree(['/admin']);
    if (role === 'Vendedor') return router.createUrlTree(['/vendedor']);
    return router.createUrlTree(['/tienda']);
  };
};
