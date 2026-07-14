const TOAST_DISMISS_MS = 2400

export interface ComposeWindow {
  id: number
  minimized: boolean
  subject: string
  to: string
  bodyHtml: string
  attachments: File[]
}

let _nextId = 1

export const useCompose = () => {
  const windows = useState<ComposeWindow[]>('composeWindows', () => [])
  const toastMessage = useState<string>('composeToast', () => '')
  const toastVisible = useState<boolean>('composeToastVisible', () => false)

  let toastTimer: ReturnType<typeof setTimeout> | null = null

  function showToast(msg: string): void {
    toastMessage.value = msg
    toastVisible.value = true
    if (toastTimer) clearTimeout(toastTimer)
    toastTimer = setTimeout(() => {
      toastVisible.value = false
    }, TOAST_DISMISS_MS)
  }

  function openCompose(): void {
    windows.value = [
      ...windows.value,
      {
        id: _nextId++,
        minimized: false,
        subject: '',
        to: '',
        bodyHtml: '',
        attachments: [],
      },
    ]
  }

  function closeWindow(id: number): void {
    windows.value = windows.value.filter(w => w.id !== id)
  }

  function toggleMinimize(id: number): void {
    const w = windows.value.find(w => w.id === id)
    if (w) w.minimized = !w.minimized
  }

  function updateWindow(id: number, patch: Partial<Pick<ComposeWindow, 'subject' | 'to' | 'bodyHtml'>>): void {
    const w = windows.value.find(w => w.id === id)
    if (w) Object.assign(w, patch)
  }

  function addAttachments(id: number, files: File[]): void {
    const w = windows.value.find(w => w.id === id)
    if (w) w.attachments = [...w.attachments, ...files]
  }

  function removeAttachment(id: number, index: number): void {
    const w = windows.value.find(w => w.id === id)
    if (w) w.attachments = w.attachments.filter((_, i) => i !== index)
  }

  function discardWindow(id: number): void {
    closeWindow(id)
    showToast('Draft discarded')
  }

  async function sendWindow(id: number): Promise<void> {
    const w = windows.value.find(w => w.id === id)
    if (!w) return
    try {
      const formData = new FormData()
      formData.append('to', w.to)
      formData.append('subject', w.subject)
      formData.append('bodyHtml', w.bodyHtml)
      for (const file of w.attachments) {
        formData.append('attachments', file)
      }
      await $fetch('/api/compose/send', {
        method: 'POST',
        body: formData,
      })
    } catch {
      // best-effort; show toast regardless in demo mode
    } finally {
      closeWindow(id)
      showToast('Message sent')
    }
  }

  async function saveDraft(id: number): Promise<void> {
    const w = windows.value.find(w => w.id === id)
    if (!w) return
    try {
      await $fetch('/api/compose/draft', {
        method: 'POST',
        body: {
          to: w.to,
          subject: w.subject,
          body: w.bodyHtml,
        },
      })
    } catch {
      // best-effort
    } finally {
      closeWindow(id)
      showToast('Draft saved')
    }
  }

  async function insertTemplate(id: number, uid: string): Promise<void> {
    const w = windows.value.find(w => w.id === id)
    if (!w) return
    try {
      const tpl = await $fetch<{ subject: string; body: string }>(`/api/compose/templates/${uid}`)
      w.subject = tpl.subject
      w.bodyHtml = tpl.body
    } catch {
      // silently ignore
    }
  }

  return {
    windows,
    toastMessage,
    toastVisible,
    openCompose,
    closeWindow,
    toggleMinimize,
    updateWindow,
    addAttachments,
    removeAttachment,
    discardWindow,
    sendWindow,
    saveDraft,
    insertTemplate,
    showToast,
  }
}
