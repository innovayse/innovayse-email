<script setup lang="ts">
// Main mail client page.
// Gmail-style collapse: only the list-panel width is transitioned.
// Reading pane is flex:1 (passive) — fills whatever space remains.

const {
  activeMailbox,
} = useMailbox()

const {
  messages,
  selectedMessage,
  currentFolder,
  folderCounts,
  searchQuery,
  loading,
  inboxUnread,
  fetchMessages,
  fetchFolderCounts,
  selectMessage,
  deselectMessage,
  switchFolder,
  search,
  startPolling,
  stopPolling,
} = useMail()

const { windows, toastMessage, toastVisible } = useCompose()

// Redirect to mailbox picker if no mailbox is selected
onMounted(async () => {
  if (!activeMailbox.value) {
    await navigateTo('/')
    return
  }
  await Promise.all([fetchMessages(), fetchFolderCounts(), fetchQuota()])
  startPolling()
})

onUnmounted(() => {
  stopPolling()
})

// Folder label map
const FOLDER_LABELS: Record<string, string> = {
  inbox: 'Inbox',
  drafts: 'Drafts',
  sent: 'Sent',
  archive: 'Archive',
  junk: 'Spam',
  templates: 'Templates',
  trash: 'Trash',
}

const folderLabel = computed(() => FOLDER_LABELS[currentFolder.value] ?? currentFolder.value)

// Selected UID for passing down to MessageList
const selectedUid = computed(() => selectedMessage.value?.uid ?? null)

// List panel is "full" when nothing is selected (Gmail-style: list fills 100% width)
const listPanelFull = computed(() => !selectedMessage.value)

// Reactive glow blooms when a message is open
const glowOn = computed(() => !!selectedMessage.value)

// Quota — fetch from Mailcow via API
const quotaData = ref<{ usedBytes: number; totalBytes: number } | undefined>()
async function fetchQuota() {
  try {
    const data = await $fetch<{ usedBytes: number; limitBytes: number }>('/api/quota')
    quotaData.value = { usedBytes: data.usedBytes, totalBytes: data.limitBytes }
  } catch {
    // fallback: use quotaMb from mailbox if available
    if (activeMailbox.value) {
      quotaData.value = { usedBytes: 0, totalBytes: (activeMailbox.value as any).quotaMb * 1024 * 1024 || 0 }
    }
  }
}

function handleSelectMessage(uid: number) {
  selectMessage(uid)
}

function handleBack() {
  deselectMessage()
}

function handleSwitchFolder(folder: string) {
  switchFolder(folder)
}

function handleSearch(query: string) {
  search(query)
}

function handleRefresh() {
  fetchMessages()
  fetchFolderCounts()
}
</script>

<template>
  <ClientOnly>
  <div class="app">
    <!-- Dot grid + ambient glows (same as reference.html) -->
    <div class="dot-grid" />
    <div class="glow glow-tl" />
    <div class="glow glow-br" />
    <div class="glow glow-tr1" />
    <div class="glow glow-tr2" />

    <div class="app-shell">
      <!-- ============ SIDEBAR ============ -->
      <MailSidebar
        :active-mailbox="activeMailbox"
        :current-folder="currentFolder"
        :inbox-unread="inboxUnread"
        :quota="quotaData"
        @switch-folder="handleSwitchFolder"
        @compose="() => {}"
      />

      <!-- ============ MAIN COLUMN ============ -->
      <div class="main-col">
        <!-- Reactive reading glow — blooms when a message is open -->
        <div class="reading-glow" :class="{ on: glowOn }" />

        <!-- Top bar -->
        <TopBar />

        <!-- Main two-pane row -->
        <div class="main-row">
          <!-- List panel — width is the ONLY thing that transitions -->
          <div
            class="list-panel"
            :class="{ full: listPanelFull }"
          >
            <MessageList
              :messages="messages"
              :selected-uid="selectedUid"
              :folder-label="folderLabel"
              :search-query="searchQuery"
              :loading="loading"
              @select="handleSelectMessage"
              @search="handleSearch"
              @refresh="handleRefresh"
            />
          </div>

          <!-- Reading pane — always flex:1, no width/flex-basis toggling -->
          <div class="reading-pane">
            <ReadingPane
              :message="selectedMessage"
              @back="handleBack"
              @reply="() => {}"
              @forward="() => {}"
              @archive="() => {}"
              @delete="() => {}"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Compose windows (Gmail-style, docked bottom-right, stackable) -->
    <ComposeWindow
      v-for="(win, idx) in windows"
      :key="win.id"
      :window="win"
      :stack-index="idx"
      :total-windows="windows.length"
    />

    <!-- Toast notification -->
    <ToastNotification :message="toastMessage" :visible="toastVisible" />
  </div>
  </ClientOnly>
</template>

<style scoped>
.app {
  position: relative;
  height: 100vh;
  width: 100%;
  background: #07080d;
  overflow: hidden;
  display: flex;
}

.app-shell {
  position: relative;
  z-index: 1;
  display: flex;
  width: 100%;
  height: 100%;
}

/* -------- Main column -------- */
.main-col {
  position: relative;
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* -------- Main two-pane row -------- */
.main-row {
  flex: 1;
  min-height: 0;
  display: flex;
  position: relative;
  z-index: 1;
}

/*
  List panel width is the ONLY thing that transitions.
  Reading pane is flex:1 (passive) and resizes smoothly every animation frame
  as the sibling's width transitions. Do not add width/flex-basis to .reading-pane.
*/
.list-panel {
  width: 340px;
  min-width: 0;
  max-width: 100%;
  flex: 0 0 auto;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  /* Only transition width (and border fade) — reading pane is passive */
  transition: width .32s cubic-bezier(.22, 1, .36, 1), border-color .2s;
}

.list-panel.full {
  width: 100%;
  border-right-color: transparent;
}

/* Reading pane: always flex:1, never has explicit width/flex-basis */
.reading-pane {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
</style>
