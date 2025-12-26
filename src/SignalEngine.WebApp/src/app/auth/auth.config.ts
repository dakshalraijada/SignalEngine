import { PassedInitialConfig } from 'angular-auth-oidc-client';
import { environment } from '../../environments/environment';

export const authConfig: PassedInitialConfig = {
  config: {
    authority: environment.identityServer.authority,
    redirectUrl: environment.identityServer.redirectUrl,
    postLogoutRedirectUri: environment.identityServer.postLogoutRedirectUri,
    clientId: environment.identityServer.clientId,
    scope: environment.identityServer.scope,
    responseType: 'code',
    silentRenewUrl: environment.identityServer.silentRenewUrl,
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    secureRoutes: [environment.apiUrl],
    // For development with HTTP
    ignoreNonceAfterRefresh: true,
    renewUserInfoAfterTokenRenew: true,
    // Log level for debugging
    logLevel: 1, // Debug
  }
};
