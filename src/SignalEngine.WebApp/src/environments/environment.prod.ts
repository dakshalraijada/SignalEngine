export const environment = {
  production: true,
  identityServer: {
    authority: 'https://identity.signalengine.io',
    clientId: 'signalengine-spa',
    redirectUrl: 'https://app.signalengine.io/callback',
    postLogoutRedirectUri: 'https://app.signalengine.io',
    silentRenewUrl: 'https://app.signalengine.io/silent-refresh',
    scope: 'openid profile email system-api'
  },
  apiUrl: 'https://api.signalengine.io'
};
