/**
 * GET /auth/callback?code=...&state=...
 *
 * Exchanges the authorization code for tokens, stores them in httpOnly cookies,
 * then redirects to the mailbox picker / inbox.
 */
export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const query = getQuery(event)

  const code = query.code as string | undefined
  if (!code) {
    throw createError({ statusCode: 400, statusMessage: 'Missing authorization code' })
  }

  // Validate state to prevent CSRF
  const storedState = getCookie(event, 'oauth_state')
  const returnedState = query.state as string | undefined
  if (!storedState || storedState !== returnedState) {
    throw createError({ statusCode: 400, statusMessage: 'Invalid state parameter' })
  }
  deleteCookie(event, 'oauth_state', { path: '/' })

  const codeVerifier = getCookie(event, 'pkce_verifier')
  if (!codeVerifier) {
    throw createError({ statusCode: 400, statusMessage: 'Missing PKCE verifier — session expired' })
  }
  deleteCookie(event, 'pkce_verifier', { path: '/' })

  // Exchange code for tokens (server-to-server, uses internal SSO URL)
  let tokenResponse: {
    access_token: string
    refresh_token?: string
    expires_in: number
    token_type: string
  }

  try {
    tokenResponse = await $fetch(`${config.ssoUrl}/connect/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'authorization_code',
        code,
        redirect_uri: config.ssoCallbackUrl as string,
        client_id: config.ssoClientId as string,
        client_secret: config.ssoClientSecret as string,
        code_verifier: codeVerifier,
      }).toString(),
    })
  } catch (err: unknown) {
    const e = err as { data?: { error?: string; error_description?: string }; statusCode?: number; message?: string }
    console.error('Token exchange failed:', config.ssoUrl, e?.data, e?.message)
    throw createError({
      statusCode: 401,
      statusMessage: e?.data?.error_description ?? e?.data?.error ?? e?.message ?? 'Token exchange failed',
    })
  }

  const { access_token, refresh_token, expires_in } = tokenResponse

  // Store access token in httpOnly cookie
  setCookie(event, 'auth_token', access_token, {
    httpOnly: true,
    secure: process.env.SECURE_COOKIES === 'true',
    sameSite: 'lax',
    maxAge: expires_in ?? 60 * 15,
    path: '/',
  })

  if (refresh_token) {
    setCookie(event, 'refresh_token', refresh_token, {
      httpOnly: true,
      secure: process.env.SECURE_COOKIES === 'true',
      sameSite: 'lax',
      maxAge: 60 * 60 * 24 * 7,
      path: '/',
    })
  }

  // Non-httpOnly flag for client-side auth check
  setCookie(event, 'authed', '1', {
    httpOnly: false,
    secure: process.env.SECURE_COOKIES === 'true',
    sameSite: 'lax',
    maxAge: 60 * 60 * 24 * 7,
    path: '/',
  })

  return sendRedirect(event, '/select-mailbox', 302)
})
