import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { switchMap, take } from 'rxjs/operators';

// URLs that should receive the Bearer token
const secureUrls = [
  'https://localhost:7005',  // System API (HTTPS)
  'http://localhost:5293',   // System API (HTTP)
  'https://localhost:7220',  // Identity Server (HTTPS)
  'http://localhost:5041'    // Identity Server (HTTP)
];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const oidcSecurityService = inject(OidcSecurityService);

  // Check if request URL should be secured
  const isSecureUrl = secureUrls.some(url => req.url.startsWith(url));

  if (!isSecureUrl) {
    return next(req);
  }

  return oidcSecurityService.getAccessToken().pipe(
    take(1),
    switchMap(token => {
      if (token) {
        const authReq = req.clone({
          setHeaders: {
            Authorization: `Bearer ${token}`
          }
        });
        return next(authReq);
      }
      return next(req);
    })
  );
};
