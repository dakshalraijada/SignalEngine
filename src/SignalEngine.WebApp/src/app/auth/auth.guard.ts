import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { filter, map, take, switchMap } from 'rxjs/operators';

export const authGuard: CanActivateFn = (route, state) => {
  const oidcSecurityService = inject(OidcSecurityService);
  const router = inject(Router);

  // Wait for OIDC to be fully configured before checking auth
  return oidcSecurityService.checkAuth().pipe(
    take(1),
    map(({ isAuthenticated }) => {
      console.log('AuthGuard - isAuthenticated:', isAuthenticated);
      if (isAuthenticated) {
        return true;
      }
      
      // Not authenticated - trigger login
      oidcSecurityService.authorize();
      return false;
    })
  );
};
