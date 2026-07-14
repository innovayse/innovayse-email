<script setup lang="ts">
import type { EmailAttachment } from '~/composables/useMail'

const props = defineProps<{
  attachment: EmailAttachment
  messageUid: number
}>()

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

const downloadUrl = computed(
  () => `/api/messages/${props.messageUid}/attachments/${props.attachment.partId}`
)
</script>

<template>
  <a
    :href="downloadUrl"
    target="_blank"
    rel="noopener"
    class="attachment-item"
  >
    <!-- file icon -->
    <div class="attach-icon">
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
        <rect x="5" y="3.5" width="12" height="15" rx="2" stroke="currentColor" stroke-width="1.6"/>
        <path d="M8 8h6M8 11.5h6M8 15h3.5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
      </svg>
    </div>

    <div class="attach-meta">
      <div class="attach-name">{{ attachment.filename }}</div>
      <div class="attach-size">{{ formatSize(attachment.size) }}</div>
    </div>

    <!-- download icon -->
    <div class="attach-download">
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
        <path d="M12 4v12M7 12l5 5 5-5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
        <path d="M5 20h14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
      </svg>
    </div>
  </a>
</template>

<style scoped>
.attachment-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.035);
  border: 1px solid rgba(255, 255, 255, 0.08);
  text-decoration: none;
  cursor: pointer;
  transition: background .15s;
}
.attachment-item:hover {
  background: rgba(255, 255, 255, 0.07);
}
.attach-icon {
  color: #8890a3;
  flex-shrink: 0;
}
.attach-meta {
  flex: 1;
  min-width: 0;
}
.attach-name {
  font-size: 13px;
  font-weight: 600;
  color: #eceef4;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.attach-size {
  font-size: 11.5px;
  color: #5f6579;
}
.attach-download {
  color: #8890a3;
  flex-shrink: 0;
}
.attachment-item:hover .attach-download {
  color: #eceef4;
}
</style>
