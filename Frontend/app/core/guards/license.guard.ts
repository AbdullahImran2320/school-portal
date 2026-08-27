import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { LicenseService } from '../../features/license/services/license.service';

export const licenseGuard: CanActivateFn = () => {
  const licenseService = inject(LicenseService);
  const router = inject(Router);

  return licenseService.getStatus().pipe(
    map(status => status.isExpired
      ? router.createUrlTree(['/license/expired'])
      : true),
    catchError(() => of(true))
  );
};
