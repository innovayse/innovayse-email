export default defineNuxtConfig({
  compatibilityDate: '2025-01-01',
  modules: ['@nuxtjs/tailwindcss'],
  app: {
    head: {
      script: [{ src: `${process.env.NUXT_PUBLIC_MAIN_URL || 'http://app.local'}/widget/header.js`, defer: true }]
    }
  },
  css: ['~/assets/css/mail.css'],
  runtimeConfig: {
    apiProxyTarget: process.env.API_PROXY_TARGET ?? 'http://localhost:4002',
    public: {
      apiBase: '/api',
      mainUrl: process.env.NUXT_PUBLIC_MAIN_URL ?? 'https://stage.innovayse.com',
    },
  },
})
