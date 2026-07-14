<script setup lang="ts">
import type { EmailMessage } from '~/composables/useMail'

const props = defineProps<{
  message: EmailMessage
  selected: boolean
}>()

const emit = defineEmits<{
  select: [uid: number]
}>()
</script>

<template>
  <div
    class="email-row"
    :class="{ selected: props.selected }"
    @click="emit('select', message.uid)"
  >
    <GradientAvatar :name="message.sender" :gradient="message.gradient" size="sm" />

    <div class="email-main">
      <div class="email-top-row">
        <span class="email-sender" :class="{ unread: message.unread }">{{ message.sender }}</span>
        <span class="email-time">{{ message.time }}</span>
      </div>
      <div class="email-subject" :class="{ unread: message.unread }">{{ message.subject }}</div>
      <div class="email-preview">{{ message.preview }}</div>
    </div>

    <div v-if="message.unread" class="unread-dot" />
  </div>
</template>

<style scoped>
.email-row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 12px 10px;
  border-radius: 13px;
  cursor: pointer;
  margin-bottom: 2px;
  border: 1px solid transparent;
}
.email-row:hover {
  background: rgba(255, 255, 255, 0.03);
}
.email-row.selected {
  background: rgba(41, 163, 232, 0.12);
  border-color: rgba(41, 163, 232, 0.3);
}
.email-main {
  flex: 1;
  min-width: 0;
}
.email-top-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  margin-bottom: 3px;
}
.email-sender {
  font-size: 13.5px;
  color: #c3c7d4;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.email-sender.unread {
  color: #f0f1f6;
  font-weight: 700;
}
.email-time {
  font-size: 11.5px;
  color: #5f6579;
  flex-shrink: 0;
}
.email-subject {
  font-size: 13px;
  color: #8890a3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  margin-bottom: 2px;
}
.email-subject.unread {
  color: #d7dae2;
  font-weight: 600;
}
.email-preview {
  font-size: 12.5px;
  color: #6b7386;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.unread-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: #29A3E8;
  flex-shrink: 0;
  margin-top: 5px;
  box-shadow: 0 0 8px #29A3E8;
}
</style>
