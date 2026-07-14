<script setup lang="ts">
import type { ComposeWindow } from '~/composables/useCompose'

const props = defineProps<{
  window: ComposeWindow
  stackIndex: number   // 0 = leftmost (oldest), increases to the right
  totalWindows: number
}>()

const {
  closeWindow,
  toggleMinimize,
  updateWindow,
  addAttachments,
  removeAttachment,
  discardWindow,
  sendWindow,
} = useCompose()

// Docking position: rightmost window is newest (highest stackIndex).
// right = 24 + (totalWindows - 1 - stackIndex) * 336
const rightPx = computed(() => 24 + (props.totalWindows - 1 - props.stackIndex) * 336)

const titleText = computed(() =>
  props.window.subject && props.window.subject.trim() ? props.window.subject : 'New message'
)

// Per-window refs
const editorRef = ref<HTMLDivElement | null>(null)
const fileInput = ref<HTMLInputElement | null>(null)

// Sync editor content when window bodyHtml changes externally (e.g. template insert)
watch(() => props.window.bodyHtml, (html) => {
  if (editorRef.value && editorRef.value.innerHTML !== html) {
    editorRef.value.innerHTML = html
  }
})

// When window goes from minimized → expanded, restore editor content
watch(() => props.window.minimized, (minimized) => {
  if (!minimized) {
    nextTick(() => {
      if (editorRef.value) {
        editorRef.value.innerHTML = props.window.bodyHtml || ''
      }
    })
  }
})

function onEditorInput() {
  if (editorRef.value) {
    updateWindow(props.window.id, { bodyHtml: editorRef.value.innerHTML })
  }
}

function execCmd(cmd: string, value?: string) {
  document.execCommand(cmd, false, value)
  editorRef.value?.focus()
}

function formatBold()      { execCmd('bold') }
function formatItalic()    { execCmd('italic') }
function formatUnderline() { execCmd('underline') }

function triggerFileInput() {
  fileInput.value?.click()
}

function onFilesSelected(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files) {
    addAttachments(props.window.id, Array.from(input.files))
    input.value = ''
  }
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function handleToggleMinimize(e: MouseEvent) {
  // Only toggle when clicking directly on the header (not buttons inside it)
  toggleMinimize(props.window.id)
}

function stopAndToggle(e: MouseEvent) {
  e.stopPropagation()
  toggleMinimize(props.window.id)
}

function stopAndClose(e: MouseEvent) {
  e.stopPropagation()
  closeWindow(props.window.id)
}
</script>

<template>
  <div
    class="compose-window"
    :class="window.minimized ? 'minimized' : 'expanded'"
    :style="{ right: rightPx + 'px', zIndex: 50 + stackIndex }"
  >
    <!-- Header -->
    <div class="compose-header" @click="handleToggleMinimize">
      <div class="compose-header-left">
        <div class="compose-badge">
          <svg width="11" height="11" viewBox="0 0 24 24" fill="none">
            <path d="M4 20l1.2-4.2L16.4 4.6a1.5 1.5 0 0 1 2.1 0l1 1a1.5 1.5 0 0 1 0 2.1L8.2 18.8 4 20Z" stroke="white" stroke-width="1.8" stroke-linejoin="round"/>
          </svg>
        </div>
        <div class="compose-title">{{ titleText }}</div>
      </div>
      <div class="compose-header-actions">
        <button class="compose-icon-btn" title="Minimize" @click="stopAndToggle">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
            <path d="M5 12h14" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
        </button>
        <button class="compose-icon-btn danger" title="Close" @click="stopAndClose">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
            <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
          </svg>
        </button>
      </div>
    </div>

    <!-- Body (hidden when minimized via CSS) -->
    <div class="compose-body">
      <!-- Fields -->
      <div class="compose-fields">
        <div class="compose-field-row">
          <span class="field-prefix">To</span>
          <input
            type="text"
            :value="window.to"
            placeholder="Recipients"
            @input="updateWindow(window.id, { to: ($event.target as HTMLInputElement).value })"
          >
        </div>
        <div class="compose-field-row">
          <input
            type="text"
            class="subject"
            placeholder="Subject"
            :value="window.subject"
            @input="updateWindow(window.id, { subject: ($event.target as HTMLInputElement).value })"
          >
        </div>
      </div>

      <!-- Contenteditable message body -->
      <div
        ref="editorRef"
        class="compose-textarea"
        contenteditable="true"
        data-placeholder="Write your message..."
        @input="onEditorInput"
      />

      <!-- Toolbar -->
      <div class="compose-toolbar">
        <button class="compose-icon-btn" title="Bold" @mousedown.prevent="formatBold">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
            <path d="M6 4h7a3.5 3.5 0 0 1 0 7H6zM6 11h8a3.5 3.5 0 0 1 0 7H6z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"/>
          </svg>
        </button>
        <button class="compose-icon-btn" title="Italic" @mousedown.prevent="formatItalic">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
            <path d="M10 5h7M8 19h7M13 5l-4 14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
          </svg>
        </button>
        <button class="compose-icon-btn" title="Underline" @mousedown.prevent="formatUnderline">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
            <path d="M6 19h12M9 19V6.5a3 3 0 0 1 6 0" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
          </svg>
        </button>
        <div class="compose-toolbar-divider" />
        <button class="compose-icon-btn" title="Attach file" @click="triggerFileInput">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
            <path d="M8 12.5l6.5-6.5a3 3 0 0 1 4.2 4.2L10 18.9a5 5 0 1 1-7-7L11.5 3.4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </button>
        <input ref="fileInput" type="file" multiple style="display:none" @change="onFilesSelected">
      </div>

      <!-- Attachment chips -->
      <div v-if="window.attachments.length" class="attachments-list">
        <div v-for="(file, i) in window.attachments" :key="i" class="attachment-chip">
          <svg width="10" height="10" viewBox="0 0 24 24" fill="none" style="flex-shrink:0;">
            <path d="M8 12.5l6.5-6.5a3 3 0 0 1 4.2 4.2L10 18.9a5 5 0 1 1-7-7L11.5 3.4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          <span class="attachment-name">{{ file.name }}</span>
          <span class="attachment-size">{{ formatSize(file.size) }}</span>
          <button class="attachment-remove" @click="removeAttachment(window.id, i)">
            <svg width="8" height="8" viewBox="0 0 24 24" fill="none"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"/></svg>
          </button>
        </div>
      </div>

      <!-- Footer -->
      <div class="compose-footer">
        <button class="compose-icon-btn danger" title="Discard" @click="discardWindow(window.id)">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
            <path d="M4 7h16M9.5 7V5a1.5 1.5 0 0 1 1.5-1.5h2A1.5 1.5 0 0 1 14.5 5v2M6 7l1 12.5A2 2 0 0 0 9 21.3h6a2 2 0 0 0 2-1.8L18 7" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>
          </svg>
        </button>
        <button class="compose-send-btn" @click="sendWindow(window.id)">
          Send
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none">
            <path d="M3 11L20 4L13 21L11 13L3 11Z" fill="white"/>
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* -------- compose window (Gmail-style, docked bottom-right, stackable) -------- */
.compose-window {
  position: fixed;
  bottom: 0;
  width: 320px;
  display: flex;
  flex-direction: column;
  background: rgba(19, 22, 35, 0.92);
  backdrop-filter: blur(26px) saturate(160%);
  -webkit-backdrop-filter: blur(26px) saturate(160%);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-top-left-radius: 14px;
  border-top-right-radius: 14px;
  box-shadow: 0 -20px 60px -20px rgba(0, 0, 0, 0.7), 0 0 0 1px rgba(255, 255, 255, 0.02) inset;
  overflow: hidden;
  transition: height .22s cubic-bezier(.22, 1, .36, 1);
  animation: composeSlideUp .3s cubic-bezier(.22, 1, .36, 1) both;
}

@keyframes composeSlideUp {
  from { opacity: 0; transform: translateY(100%); }
  to   { opacity: 1; transform: translateY(0); }
}

.compose-window.expanded  { height: 440px; }
.compose-window.minimized { height: 44px; }

/* Header */
.compose-header {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 12px;
  background: rgba(255, 255, 255, 0.03);
  cursor: pointer;
  user-select: none;
}

.compose-window.expanded .compose-header {
  border-bottom: 1px solid rgba(255, 255, 255, 0.07);
}

.compose-header-left {
  display: flex;
  align-items: center;
  gap: 9px;
  min-width: 0;
}

.compose-badge {
  width: 22px;
  height: 22px;
  border-radius: 7px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.compose-title {
  font-size: 13px;
  font-weight: 700;
  color: #f0f1f6;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.compose-header-actions {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

/* Icon buttons */
.compose-icon-btn {
  width: 26px;
  height: 26px;
  border-radius: 7px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background .15s, color .15s;
}

.compose-icon-btn:hover        { background: rgba(255, 255, 255, 0.08); color: #e6e8f0; }
.compose-icon-btn.danger:hover { color: #f36565; }

/* Body — hidden when minimized */
.compose-body {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.compose-window.minimized .compose-body {
  display: none;
}

/* Fields */
.compose-fields {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  padding: 0 16px;
}

.compose-field-row {
  display: flex;
  align-items: center;
  gap: 9px;
  height: 38px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
}

.compose-field-row .field-prefix {
  font-size: 12.5px;
  color: #7d8496;
  flex-shrink: 0;
}

.compose-field-row input {
  flex: 1;
  border: none;
  background: transparent;
  color: #eceef4;
  font-size: 13px;
  font-family: inherit;
  outline: none;
}

.compose-field-row input::placeholder { color: #565d70; }
.compose-field-row input.subject { font-weight: 600; }

/* Contenteditable body */
.compose-textarea {
  flex: 1;
  min-height: 120px;
  border: none;
  background: transparent;
  color: #d7dae2;
  font-size: 13.5px;
  font-family: inherit;
  line-height: 1.7;
  outline: none;
  padding: 14px 16px;
  width: 100%;
  overflow-y: auto;
}

.compose-textarea:empty::before {
  content: attr(data-placeholder);
  color: #565d70;
  pointer-events: none;
}

.compose-textarea :deep(ul),
.compose-textarea :deep(ol) {
  padding-left: 18px;
  margin: 4px 0;
}

.compose-textarea :deep(a) {
  color: #29A3E8;
  text-decoration: underline;
}

/* Toolbar */
.compose-toolbar {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 2px;
  margin: 0 16px 10px;
  padding: 5px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.06);
  width: fit-content;
}

.compose-toolbar .compose-icon-btn svg {
  width: 12px;
  height: 12px;
}

.compose-toolbar-divider {
  width: 1px;
  height: 14px;
  background: rgba(255, 255, 255, 0.1);
  margin: 0 4px;
}

/* Attachment chips */
.attachments-list {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 0 16px 8px;
}

.attachment-chip {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 4px 8px;
  border-radius: 7px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: #c3c7d4;
  font-size: 11px;
}

.attachment-name {
  max-width: 90px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.attachment-size { color: #5f6579; }

.attachment-remove {
  width: 14px;
  height: 14px;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: color .15s;
}

.attachment-remove:hover { color: #f36565; }

/* Footer */
.compose-footer {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: rgba(255, 255, 255, 0.02);
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}

.compose-send-btn {
  height: 34px;
  padding: 0 18px;
  border-radius: 9px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
  font-size: 13px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 7px;
  cursor: pointer;
  box-shadow: 0 8px 20px -8px rgba(41, 163, 232, 0.35);
  transition: transform .18s;
  border: none;
  font-family: inherit;
}

.compose-send-btn:hover  { transform: translateY(-2px); }
.compose-send-btn:active { transform: translateY(0); }
</style>
