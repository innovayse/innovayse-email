export interface EmailMessage {
  uid: number
  messageId: string
  folder: string
  sender: string
  senderEmail: string
  subject: string
  preview: string
  bodyHtml: string
  time: string
  date: string
  unread: boolean
  gradient: string
  initials: string
  attachments?: EmailAttachment[]
}

export interface EmailAttachment {
  partId: string
  filename: string
  contentType: string
  size: number
}

export interface FolderCount {
  folder: string
  total: number
  unread: number
}

export interface MessagesPage {
  items: EmailMessage[]
  total: number
  page: number
  pageSize: number
}

const POLL_INTERVAL_MS = 20_000

function mapMessage(raw: any): EmailMessage {
  return {
    uid: raw.uid,
    messageId: raw.messageId ?? '',
    folder: raw.folder ?? 'inbox',
    sender: raw.sender ?? 'Unknown',
    senderEmail: raw.senderEmail ?? '',
    subject: raw.subject ?? '(no subject)',
    preview: raw.preview ?? '',
    bodyHtml: raw.bodyHtml ?? '',
    time: raw.time ?? '',
    date: raw.date ?? '',
    unread: raw.unread ?? false,
    gradient: '',
    initials: '',
    attachments: raw.attachments,
  }
}

export const useMail = () => {
  const messages = useState<EmailMessage[]>('mailMessages', () => [])
  const selectedMessage = useState<EmailMessage | null>('selectedMessage', () => null)
  const currentFolder = useState<string>('currentFolder', () => 'inbox')
  const folderCounts = useState<FolderCount[]>('folderCounts', () => [])
  const searchQuery = useState<string>('searchQuery', () => '')
  const loading = useState<boolean>('mailLoading', () => false)
  const page = useState<number>('mailPage', () => 1)
  const total = useState<number>('mailTotal', () => 0)
  const pageSize = 30

  let pollTimer: ReturnType<typeof setTimeout> | null = null

  async function fetchMessages(): Promise<void> {
    loading.value = true
    try {
      const params: Record<string, string | number> = {
        folder: currentFolder.value,
        page: page.value,
        pageSize,
      }
      if (searchQuery.value) {
        params.q = searchQuery.value
      }

      const raw = await $fetch<any[]>('/api/messages', { params })
      messages.value = (raw ?? []).map(mapMessage)
      total.value = messages.value.length
    } catch {
      // silently handle polling errors
    } finally {
      loading.value = false
    }
  }

  async function fetchFolderCounts(): Promise<void> {
    try {
      const data = await $fetch<FolderCount[]>('/api/messages/counts')
      folderCounts.value = data
    } catch {
      // silently handle
    }
  }

  async function selectMessage(uid: number): Promise<void> {
    const msg = messages.value.find((m) => m.uid === uid)
    if (msg) {
      msg.unread = false
      // Update unread count locally
      const inboxCount = folderCounts.value.find((f) => f.folder === 'inbox')
      if (inboxCount && msg.folder === 'inbox') {
        inboxCount.unread = Math.max(0, inboxCount.unread - 1)
      }
    }
    // Fetch full message detail (with body + attachments)
    try {
      const detail = await $fetch<any>(`/api/messages/${currentFolder.value}/${uid}`)
      selectedMessage.value = mapMessage(detail)
    } catch {
      // Fallback to summary if detail fetch fails
      if (msg) selectedMessage.value = msg
    }
  }

  function deselectMessage(): void {
    selectedMessage.value = null
  }

  function switchFolder(folder: string): void {
    currentFolder.value = folder
    selectedMessage.value = null
    page.value = 1
    searchQuery.value = ''
    fetchMessages()
    fetchFolderCounts()
  }

  async function search(query: string): Promise<void> {
    searchQuery.value = query
    page.value = 1
    selectedMessage.value = null
    await fetchMessages()
  }

  function startPolling(): void {
    stopPolling()
    pollTimer = setInterval(() => {
      fetchMessages()
      fetchFolderCounts()
    }, POLL_INTERVAL_MS)
  }

  function stopPolling(): void {
    if (pollTimer !== null) {
      clearInterval(pollTimer)
      pollTimer = null
    }
  }

  const inboxUnread = computed<number>(() => {
    const entry = folderCounts.value?.find((f) =>
      String(f.folder).toLowerCase() === 'inbox' || f.folder === 0)
    if (entry) return entry.unread ?? 0
    // Fallback: count from loaded messages if on inbox
    if (currentFolder.value === 'inbox' && messages.value) {
      return messages.value.filter((m) => !m.isRead).length
    }
    return 0
  })

  return {
    messages,
    selectedMessage,
    currentFolder,
    folderCounts,
    searchQuery,
    loading,
    page,
    total,
    pageSize,
    inboxUnread,
    fetchMessages,
    fetchFolderCounts,
    selectMessage,
    deselectMessage,
    switchFolder,
    search,
    startPolling,
    stopPolling,
  }
}
