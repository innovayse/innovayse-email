<script setup lang="ts">
import type { Mailbox } from '~/composables/useMailbox'

const props = defineProps<{
  activeMailbox: Mailbox | null
  currentFolder: string
  inboxUnread: number
  quota?: { usedBytes: number; totalBytes: number }
}>()

const emit = defineEmits<{
  switchFolder: [key: string]
  compose: []
  logout: []
}>()

const { openCompose } = useCompose()

const FOLDERS = [
  {
    key: 'inbox', label: 'Inbox',
    icon: '<path d="M3 8.5L12 13.5L21 8.5" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"/><rect x="3" y="5.5" width="18" height="13" rx="3" stroke="currentColor" stroke-width="1.7"/>',
  },
  {
    key: 'drafts', label: 'Drafts',
    icon: '<path d="M4 20l1.2-4.2L16.4 4.6a1.5 1.5 0 0 1 2.1 0l1 1a1.5 1.5 0 0 1 0 2.1L8.2 18.8 4 20Z" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>',
  },
  {
    key: 'sent', label: 'Sent',
    icon: '<path d="M3 11L20 4L13 21L11 13L3 11Z" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>',
  },
  {
    key: 'archive', label: 'Archive',
    icon: '<rect x="3" y="5" width="18" height="4.5" rx="1.4" stroke="currentColor" stroke-width="1.6"/><path d="M4.5 9.5V17a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2V9.5" stroke="currentColor" stroke-width="1.6"/><path d="M10 13.2h4" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>',
  },
  {
    key: 'junk', label: 'Spam',
    icon: '<circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.6"/><path d="M12 8v4M12 16h.01" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>',
  },
  {
    key: 'templates', label: 'Templates',
    icon: '<rect x="5" y="3.5" width="12" height="15" rx="2" stroke="currentColor" stroke-width="1.6"/><path d="M8 8h6M8 11.5h6M8 15h3.5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>',
  },
  {
    key: 'trash', label: 'Trash',
    icon: '<path d="M4 7h16M9.5 7V5a1.5 1.5 0 0 1 1.5-1.5h2A1.5 1.5 0 0 1 14.5 5v2M6 7l1 12.5A2 2 0 0 0 9 21.3h6a2 2 0 0 0 2-1.8L18 7" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>',
  },
]

const profileInitials = computed(() => {
  if (!props.activeMailbox?.displayName) return '?'
  const parts = props.activeMailbox.displayName.trim().split(/\s+/)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return props.activeMailbox.displayName.slice(0, 2).toUpperCase()
})

const storagePercent = computed(() => {
  if (!props.quota) return 12 // default demo
  return Math.round((props.quota.usedBytes / props.quota.totalBytes) * 100)
})

function formatBytes(bytes: number): string {
  const mb = bytes / (1024 * 1024)
  if (mb >= 1024) return `${(mb / 1024).toFixed(1)} GB`
  return `${Math.round(mb)} MB`
}

const storageLabel = computed(() => {
  if (!props.quota) return '0 MB used'
  return `${formatBytes(props.quota.usedBytes)} of ${formatBytes(props.quota.totalBytes)} used`
})

function handleCompose() {
  openCompose()
  emit('compose')
}
</script>

<template>
  <div class="sidebar">
    <SidebarParticles />

    <!-- Brand row -->
    <div class="brand-row">
      <div class="brand-mark">
        <svg width="17" height="17" viewBox="0 0 24 24" fill="none">
          <path d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z" fill="white" fill-opacity="0.95"/>
        </svg>
      </div>
      <div class="brand-name">Innovayse Mail</div>
    </div>

    <!-- Profile card -->
    <div class="profile-card">
      <div class="profile-avatar">{{ profileInitials }}</div>
      <div style="min-width:0;flex:1;">
        <div class="profile-name">{{ activeMailbox?.displayName ?? 'Loading…' }}</div>
        <div class="profile-email">{{ activeMailbox?.email ?? '' }}</div>
      </div>
      <button class="settings-btn" title="Settings">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="2.8" stroke="currentColor" stroke-width="1.7"/>
          <path d="M19.4 13.5c.1-.5.1-1 0-1.5l1.9-1.5-2-3.4-2.3.7c-.4-.3-.8-.6-1.3-.8L15.3 4h-4l-.4 2.9c-.5.2-.9.5-1.3.8l-2.3-.7-2 3.4L7.2 12c-.1.5-.1 1 0 1.5l-1.9 1.5 2 3.4 2.3-.7c.4.3.8.6 1.3.8l.4 3h4l.4-2.9c.5-.2.9-.5 1.3-.8l2.3.7 2-3.4-1.9-1.6Z" stroke="currentColor" stroke-width="1.3" stroke-linejoin="round"/>
        </svg>
      </button>
      <button class="settings-btn" title="Log out" @click="emit('logout')">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
          <path d="M15 17l5-5-5-5M20 12H9M12 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h6" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </button>
    </div>

    <!-- Compose button -->
    <button class="compose-btn" @click="handleCompose">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
        <path d="M12 5v14M5 12h14" stroke="white" stroke-width="2.2" stroke-linecap="round"/>
      </svg>
      Compose
    </button>

    <!-- Folder nav -->
    <nav class="nav-list">
      <button
        v-for="folder in FOLDERS"
        :key="folder.key"
        class="nav-item"
        :class="{ active: currentFolder === folder.key }"
        @click="emit('switchFolder', folder.key)"
      >
        <div class="nav-item-label">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" v-html="folder.icon" />
          <span>{{ folder.label }}</span>
        </div>
        <span v-if="folder.key === 'inbox' && inboxUnread > 0" class="nav-badge">
          {{ inboxUnread }}
        </span>
      </button>
    </nav>

    <!-- Storage bar -->
    <div class="storage">
      <div class="storage-track">
        <div class="storage-fill" :style="{ width: storagePercent + '%' }" />
      </div>
      <div class="storage-label">{{ storageLabel }}</div>
    </div>
  </div>
</template>

<style scoped>
.sidebar {
  position: relative;
  flex-shrink: 0;
  width: 224px;
  height: 100%;
  background: rgba(14, 16, 26, 0.55);
  backdrop-filter: blur(24px) saturate(160%);
  -webkit-backdrop-filter: blur(24px) saturate(160%);
  border-right: 1px solid rgba(255, 255, 255, 0.07);
  display: flex;
  flex-direction: column;
  padding: 22px 16px;
  overflow: hidden;
}
.sidebar > * {
  position: relative;
  z-index: 1;
}

.brand-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 4px 8px 20px;
}
.brand-mark {
  width: 34px;
  height: 34px;
  border-radius: 10px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 6px 16px rgba(41, 163, 232, 0.35);
}
.brand-name {
  font-size: 16.5px;
  font-weight: 800;
  color: #f3f4f8;
  letter-spacing: -0.01em;
}

.profile-card {
  display: flex;
  align-items: center;
  gap: 12px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.06);
  border-radius: 14px;
  padding: 12px;
  margin-bottom: 16px;
}
.profile-avatar {
  width: 38px;
  height: 38px;
  border-radius: 11px;
  background: linear-gradient(135deg, #3B82F6, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  font-size: 13px;
  font-weight: 700;
  color: white;
}
.profile-name {
  font-size: 13.5px;
  font-weight: 700;
  color: #eceef4;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.profile-email {
  font-size: 11.5px;
  color: #7d8496;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.settings-btn {
  padding: 4px;
  width: auto;
  height: auto;
  background: transparent;
  border: none;
  cursor: pointer;
  color: #8890a3;
  display: flex;
  align-items: center;
  transition: color .15s;
}
.settings-btn:hover { color: #e6e8f0; }

.compose-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 46px;
  border-radius: 13px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
  font-size: 14px;
  font-weight: 700;
  cursor: pointer;
  box-shadow: 0 10px 24px -8px rgba(41, 163, 232, 0.35);
  transition: transform .18s, box-shadow .18s;
  margin-bottom: 18px;
  border: none;
  width: 100%;
  font-family: inherit;
}
.compose-btn:hover { transform: translateY(-2px); box-shadow: 0 14px 30px -8px rgba(41, 163, 232, 0.35); }
.compose-btn:active { transform: translateY(0); }

.nav-list {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}
.nav-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  border-radius: 11px;
  background: transparent;
  color: #8890a3;
  font-size: 13.5px;
  font-weight: 600;
  cursor: pointer;
  border: none;
  font-family: inherit;
  text-align: left;
  width: 100%;
  transition: background .15s, color .15s;
}
.nav-item:hover { background: rgba(255,255,255,.04); color: #d0d3df; }
.nav-item.active { background: rgba(255, 255, 255, 0.06); color: #f0f1f6; font-weight: 700; }
.nav-item-label {
  display: flex;
  align-items: center;
  gap: 11px;
}
.nav-badge {
  font-size: 11px;
  font-weight: 700;
  color: white;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  border-radius: 10px;
  padding: 2px 8px;
}

.storage {
  padding: 14px 10px 4px;
}
.storage-track {
  height: 5px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.07);
  overflow: hidden;
  margin-bottom: 8px;
}
.storage-fill {
  height: 100%;
  border-radius: 4px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
}
.storage-label {
  font-size: 11px;
  color: #5f6579;
}
</style>
