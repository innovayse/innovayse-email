<script setup lang="ts">
import type { EmailMessage } from '~/composables/useMail'

const props = defineProps<{
  message: EmailMessage | null
}>()

const emit = defineEmits<{
  back: []
  reply: [uid: number]
  forward: [uid: number]
  archive: [uid: number]
  delete: [uid: number]
}>()
</script>

<template>
  <!-- Empty state when nothing selected -->
  <div v-if="!message" class="empty-pane">
    <div class="empty-pane-icon">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" style="color:#565d70;">
        <rect x="3" y="5.5" width="18" height="13" rx="3" stroke="currentColor" stroke-width="1.6"/>
        <path d="M3 8.5L12 13.5L21 8.5" stroke="currentColor" stroke-width="1.6"/>
      </svg>
    </div>
    <div style="font-size:14.5px;color:#565d70;">Select a message to read</div>
  </div>

  <!-- Message content -->
  <div v-else class="reading-scroll">
    <!-- Back link -->
    <div class="back-link" @click="emit('back')">
      <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
        <path d="M19 12H5M5 12L11 6M5 12L11 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
      </svg>
      Back to inbox
    </div>

    <!-- Header: subject + action icons -->
    <div class="reading-header">
      <div class="reading-subject">{{ message.subject }}</div>
      <div class="reading-actions">
        <!-- Reply -->
        <button class="icon-btn" title="Reply" @click="emit('reply', message.uid)">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
            <path d="M9 6L3 12L9 18" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M3 12h11a5 5 0 0 1 5 5v1" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>
          </svg>
        </button>
        <!-- Forward -->
        <button class="icon-btn" title="Forward" @click="emit('forward', message.uid)">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
            <path d="M15 6l6 6-6 6" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
            <path d="M21 12H10a5 5 0 0 0-5 5v1" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>
          </svg>
        </button>
        <!-- Archive -->
        <button class="icon-btn" title="Archive" @click="emit('archive', message.uid)">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
            <rect x="3" y="5" width="18" height="4.5" rx="1.4" stroke="currentColor" stroke-width="1.6"/>
            <path d="M4.5 9.5V17a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2V9.5" stroke="currentColor" stroke-width="1.6"/>
          </svg>
        </button>
        <!-- Delete -->
        <button class="icon-btn danger" title="Delete" @click="emit('delete', message.uid)">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
            <path d="M4 7h16M9.5 7V5a1.5 1.5 0 0 1 1.5-1.5h2A1.5 1.5 0 0 1 14.5 5v2M6 7l1 12.5A2 2 0 0 0 9 21.3h6a2 2 0 0 0 2-1.8L18 7" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- Sender meta -->
    <div class="reading-meta">
      <GradientAvatar :name="message.sender" :gradient="message.gradient" size="lg" />
      <div style="flex:1;min-width:0;">
        <div class="reading-sender">{{ message.sender }}</div>
        <div class="reading-email">{{ message.senderEmail }}</div>
      </div>
      <div class="reading-time">{{ message.time }}</div>
    </div>

    <!-- Body -->
    <div v-if="message.bodyHtml" class="reading-body" v-html="message.bodyHtml" />
    <div v-else class="reading-body"><p>{{ message.preview || '' }}</p></div>

    <!-- Attachments -->
    <div v-if="message.attachments && message.attachments.length" class="attachments-section">
      <div class="attachments-label">Attachments</div>
      <div class="attachments-list">
        <AttachmentItem
          v-for="att in message.attachments"
          :key="att.partId"
          :attachment="att"
          :message-uid="message.uid"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.empty-pane {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  text-align: center;
  color: #565d70;
  height: 100%;
}
.empty-pane-icon {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.04);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 16px;
}
.reading-scroll {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  padding: 28px 32px;
  animation: fadeIn .3s ease both;
}
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

.back-link {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #8890a3;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  margin-bottom: 18px;
  width: fit-content;
  border: none;
  background: none;
  padding: 0;
  transition: color .15s;
}
.back-link:hover { color: #e6e8f0; }

.reading-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 22px;
}
.reading-subject {
  font-size: 21px;
  font-weight: 800;
  color: #f5f6fa;
  letter-spacing: -0.01em;
  line-height: 1.3;
  min-width: 0;
}
.reading-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-shrink: 0;
}
.icon-btn {
  width: 34px;
  height: 34px;
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
.icon-btn:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #e6e8f0;
}
.icon-btn.danger:hover { color: #f36565; }

.reading-meta {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-bottom: 24px;
  margin-bottom: 24px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
}
.reading-sender {
  font-size: 14.5px;
  font-weight: 700;
  color: #eceef4;
}
.reading-email {
  font-size: 12.5px;
  color: #8890a3;
}
.reading-time {
  font-size: 12.5px;
  color: #6b7386;
  flex-shrink: 0;
}
.reading-body {
  font-size: 15px;
  line-height: 1.75;
  color: #c3c7d4;
  max-width: 640px;
}
.reading-body :deep(p) { margin: 0 0 16px; }

.attachments-section {
  margin-top: 28px;
  max-width: 640px;
}
.attachments-label {
  font-size: 12px;
  font-weight: 700;
  color: #8890a3;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  margin-bottom: 10px;
}
.attachments-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}
</style>
