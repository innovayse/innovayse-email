<script setup lang="ts">
import type { Mailbox } from '~/composables/useMailbox'

const props = defineProps<{
  mailbox: Mailbox
}>()

const emit = defineEmits<{
  select: [email: string]
}>()

const initials = computed(() => {
  const name = props.mailbox.displayName || props.mailbox.email
  const parts = name.trim().split(/\s+/)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return name.slice(0, 2).toUpperCase()
})

const GRADIENTS = [
  'linear-gradient(135deg,#29A3E8,#8B5CF6)',
  'linear-gradient(135deg,#22D3C9,#3B82F6)',
  'linear-gradient(135deg,#EC4899,#8B5CF6)',
]

const gradient = computed(() => {
  let hash = 0
  for (let i = 0; i < props.mailbox.email.length; i++) {
    hash = (hash * 31 + props.mailbox.email.charCodeAt(i)) >>> 0
  }
  return GRADIENTS[hash % GRADIENTS.length]
})

function formatQuota(mailbox: Mailbox): string {
  if (!mailbox.quota) return ''
  const usedMB = (mailbox.quota.usedBytes / (1024 * 1024)).toFixed(0)
  const totalGB = (mailbox.quota.totalBytes / (1024 * 1024 * 1024)).toFixed(0)
  return `${usedMB} MB / ${totalGB} GB`
}
</script>

<template>
  <button class="mailbox-card" @click="emit('select', mailbox.email)">
    <div class="card-avatar" :style="{ background: gradient }">
      {{ initials }}
    </div>
    <div class="card-info">
      <div class="card-name">{{ mailbox.displayName || mailbox.email }}</div>
      <div class="card-email">{{ mailbox.email }}</div>
      <div v-if="mailbox.quota" class="card-quota">{{ formatQuota(mailbox) }}</div>
    </div>
    <!-- arrow -->
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" style="color:#565d70;flex-shrink:0;">
      <path d="M9 18l6-6-6-6" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>
  </button>
</template>

<style scoped>
.mailbox-card {
  display: flex;
  align-items: center;
  gap: 16px;
  width: 100%;
  padding: 18px 20px;
  border-radius: 20px;
  background: rgba(14, 16, 26, 0.55);
  backdrop-filter: blur(24px) saturate(160%);
  -webkit-backdrop-filter: blur(24px) saturate(160%);
  border: 1px solid rgba(255, 255, 255, 0.07);
  cursor: pointer;
  font-family: inherit;
  text-align: left;
  transition: background .2s, border-color .2s, transform .18s;
}
.mailbox-card:hover {
  background: rgba(20, 23, 38, 0.75);
  border-color: rgba(41, 163, 232, 0.3);
  transform: translateY(-2px);
}
.card-avatar {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 16px;
  font-weight: 700;
  color: white;
  flex-shrink: 0;
}
.card-info {
  flex: 1;
  min-width: 0;
}
.card-name {
  font-size: 15px;
  font-weight: 700;
  color: #eceef4;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-bottom: 2px;
}
.card-email {
  font-size: 12.5px;
  color: #8890a3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.card-quota {
  font-size: 11.5px;
  color: #565d70;
  margin-top: 2px;
}
</style>
