export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig()
  const target = config.apiProxyTarget as string

  const path = event.path
  const targetUrl = `${target}${path}`

  // Build headers, forwarding all except host
  const reqHeaders: Record<string, string> = {}
  const incomingHeaders = getRequestHeaders(event)
  for (const [key, value] of Object.entries(incomingHeaders)) {
    if (key === 'host') continue
    if (value) reqHeaders[key] = value as string
  }

  // Inject Bearer token from httpOnly auth_token cookie
  const authToken = getCookie(event, 'auth_token')
  if (authToken) {
    reqHeaders['authorization'] = `Bearer ${authToken}`
  }

  // Inject active mailbox from cookie
  const mailboxEmail = getCookie(event, 'innovayse_active_mailbox')
  if (mailboxEmail) {
    reqHeaders['x-mailbox-email'] = mailboxEmail
  }

  const method = getMethod(event)
  const hasBody = method !== 'GET' && method !== 'HEAD'

  // For multipart/form-data (file uploads), stream the raw request body
  // For other content types, use readRawBody
  const contentType = incomingHeaders['content-type'] ?? ''
  let body: any = undefined
  if (hasBody) {
    if (contentType.includes('multipart/form-data')) {
      // Stream the raw body from the incoming request
      body = getRequestWebStream(event)
    } else {
      body = await readRawBody(event)
    }
  }

  const response = await fetch(targetUrl, {
    method,
    headers: reqHeaders,
    body,
    redirect: 'manual',
    // @ts-ignore - duplex required for streaming body
    duplex: 'half',
  })

  // Forward response headers
  for (const [key, value] of response.headers.entries()) {
    if (key === 'transfer-encoding') continue
    setResponseHeader(event, key, value)
  }

  setResponseStatus(event, response.status)

  const respContentType = response.headers.get('content-type') ?? ''
  if (respContentType.includes('application/json')) {
    return await response.json()
  }
  return await response.text()
})
