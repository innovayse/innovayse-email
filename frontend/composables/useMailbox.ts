export interface Mailbox {
  email: string
  displayName: string
  quotaMb?: number
}

export const useMailbox = () => {
  const activeMailbox = useState<Mailbox | null>('activeMailbox', () => null)
  const loading = useState<boolean>('mailboxLoading', () => false)
  const error = useState<string | null>('mailboxError', () => null)

  async function fetchActiveMailbox(): Promise<Mailbox | null> {
    loading.value = true
    error.value = null
    try {
      const data = await $fetch<Mailbox>('/api/mailboxes/active')
      activeMailbox.value = data
      return data
    } catch (e: any) {
      // Re-throw 401 so the caller can redirect to /login
      const status = e?.status || e?.statusCode || e?.response?.status
      if (status === 401) {
        throw e
      }
      error.value = e?.message ?? 'Failed to load mailbox'
      return null
    } finally {
      loading.value = false
    }
  }

  return {
    activeMailbox,
    loading,
    error,
    fetchActiveMailbox,
  }
}
