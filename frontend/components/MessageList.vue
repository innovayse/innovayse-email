<script setup lang="ts">
import type { EmailMessage } from '~/composables/useMail'

const props = defineProps<{
  messages: EmailMessage[]
  selectedUid: number | null
  folderLabel: string
  searchQuery: string
  loading: boolean
}>()

const emit = defineEmits<{
  select: [uid: number]
  search: [query: string]
  refresh: []
}>()

function onSearchInput(e: Event) {
  emit('search', (e.target as HTMLInputElement).value)
}
</script>

<template>
  <div class="list-panel-inner">
    <div class="list-header">
      <div class="list-header-row">
        <div class="list-title">{{ folderLabel }}</div>
        <div style="display:flex;align-items:center;gap:4px;">
          <!-- filter icon -->
          <button class="list-icon-btn" title="Filter">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
              <path d="M4 6h16M7 12h10M10 18h4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>
            </svg>
          </button>
          <!-- refresh icon -->
          <button class="list-icon-btn" title="Refresh" @click="emit('refresh')">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
              <path d="M4 4v6h6M20 20v-6h-6" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
              <path d="M5 14a8 8 0 0 0 14.5 3M19 10A8 8 0 0 0 4.5 7" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>
            </svg>
          </button>
        </div>
      </div>

      <div class="search-wrap">
        <svg class="search-icon" width="15" height="15" viewBox="0 0 24 24" fill="none">
          <circle cx="11" cy="11" r="7" stroke="currentColor" stroke-width="1.8"/>
          <path d="M20 20l-4-4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
        </svg>
        <input
          class="search-input"
          type="text"
          placeholder="Search mail"
          :value="searchQuery"
          @input="onSearchInput"
        >
      </div>
    </div>

    <div class="list-scroll">
      <!-- Loading state -->
      <div v-if="loading && messages.length === 0" class="empty-folder">
        <svg width="34" height="34" viewBox="0 0 24 24" fill="none" style="margin-bottom:12px;opacity:0.4;animation:spin 1s linear infinite;">
          <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.6" stroke-dasharray="28 8"/>
        </svg>
        <div style="font-size:13.5px;">Loading…</div>
      </div>

      <!-- Empty folder -->
      <div v-else-if="messages.length === 0" class="empty-folder">
        <svg width="34" height="34" viewBox="0 0 24 24" fill="none" style="margin-bottom:12px;opacity:0.6;">
          <rect x="3" y="5.5" width="18" height="13" rx="3" stroke="currentColor" stroke-width="1.6"/>
          <path d="M3 8.5L12 13.5L21 8.5" stroke="currentColor" stroke-width="1.6"/>
        </svg>
        <div style="font-size:13.5px;">This folder is empty</div>
      </div>

      <!-- Message rows -->
      <template v-else>
        <MessageRow
          v-for="msg in messages"
          :key="msg.uid"
          :message="msg"
          :selected="msg.uid === selectedUid"
          @select="emit('select', $event)"
        />
      </template>
    </div>
  </div>
</template>

<style scoped>
.list-panel-inner {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
}
.list-header {
  flex-shrink: 0;
  padding: 18px 20px 14px;
}
.list-header-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}
.list-title {
  font-size: 20px;
  font-weight: 800;
  color: #f5f6fa;
  letter-spacing: -0.01em;
}
.list-icon-btn {
  width: 30px;
  height: 30px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background .15s, color .15s;
}
.list-icon-btn:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #e6e8f0;
}
.search-wrap {
  position: relative;
}
.search-icon {
  position: absolute;
  left: 13px;
  top: 50%;
  transform: translateY(-50%);
  color: #5a6178;
  pointer-events: none;
}
.search-input {
  width: 100%;
  height: 38px;
  padding: 0 14px 0 38px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.035);
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: #eceef4;
  font-size: 13.5px;
  font-family: inherit;
  outline: none;
}
.search-input::placeholder { color: #5a6178; }
.search-input:focus {
  border-color: #29A3E8;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.22);
}
.list-scroll {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 0 10px 14px;
}
.empty-folder {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  color: #565d70;
  padding: 60px 20px;
}
@keyframes spin { to { transform: rotate(360deg); } }
</style>
