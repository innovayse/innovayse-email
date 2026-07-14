export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  modules: ['@nuxtjs/tailwindcss'],
  css: ['~/assets/css/mail.css'],
  runtimeConfig: {
    apiProxyTarget: process.env.API_PROXY_TARGET ?? 'http://localhost:4002',
    ssoUrl: process.env.SSO_URL ?? 'http://sso-api:8080',
    ssoClientId: process.env.SSO_CLIENT_ID ?? 'innovayse-email',
    ssoClientSecret: process.env.SSO_CLIENT_SECRET ?? 'dev-secret-email',
    ssoCallbackUrl: process.env.SSO_CALLBACK_URL ?? 'http://localhost:4003/auth/callback',
    public: {
      apiBase: '/api',
      ssoPublicUrl: process.env.SSO_PUBLIC_URL ?? 'http://localhost:4001',
    },
  },
})
