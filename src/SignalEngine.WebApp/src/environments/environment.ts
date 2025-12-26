export const environment = {
  production: false,
  identityServer: {
    authority: 'https://localhost:7220',
    clientId: 'signalengine-spa',
    redirectUrl: 'https://localhost:4200/callback',
    postLogoutRedirectUri: 'https://localhost:4200',
    silentRenewUrl: 'https://localhost:4200/silent-refresh',
    scope: 'openid profile email system-api'
  },
  apiUrl: 'https://localhost:7220/api'
};
