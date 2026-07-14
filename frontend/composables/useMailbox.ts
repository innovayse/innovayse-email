export interface Mailbox {
  email: string
  displayName: string
  quota?: {
    usedBytes: number
    totalBytes: number
  }
}

const MAILBOX_COOKIE = 'innovayse_active_mailbox'

export const useMailbox = () => {
  const activeMailbox = useState<Mailbox | null>('activeMailbox', () => null)
  const mailboxes = useState<Mailbox[]>('mailboxes', () => [])
  const loading = useState<boolean>('mailboxLoading', () => false)
  const error = useState<string | null>('mailboxError', () => null)

  const mailboxCookie = useCookie<string | null>(MAILBOX_COOKIE, {
    maxAge: 60 * 60 * 24 * 30, // 30 days
    default: () => null,
  })

  async function fetchMailboxes(): Promise<Mailbox[]> {
    loading.value = true
    error.value = null
    try {
      const data = await $fetch<Mailbox[]>('/api/mailboxes')
      mailboxes.value = data

      // Restore previously selected mailbox from cookie
      if (mailboxCookie.value) {
        const saved = data.find((m) => m.email === mailboxCookie.value)
        if (saved) activeMailbox.value = saved
      }

      return data
    } catch (e: any) {
      // Re-throw 401 so the caller can redirect to SSO
      const status = e?.status || e?.statusCode || e?.response?.status
      if (status === 401) {
        throw e
      }
      error.value = e?.message ?? 'Failed to load mailboxes'
      return []
    } finally {
      loading.value = false
    }
  }

  function selectMailbox(email: string): void {
    const found = mailboxes.value.find((m) => m.email === email)
    if (found) {
      activeMailbox.value = found
      mailboxCookie.value = email
    }
  }

  return {
    mailboxes,
    activeMailbox,
    loading,
    error,
    fetchMailboxes,
    selectMailbox,
  }
}
