<script setup lang="ts">
const { isOpen, composeTo, composeSubject, composeBody, attachments, closeCompose, sendMessage, saveDraft, discardDraft, addAttachments, removeAttachment } = useCompose()

const fileInput = ref<HTMLInputElement | null>(null)
const editorRef = ref<HTMLDivElement | null>(null)

function triggerFileInput() {
  fileInput.value?.click()
}

function onFilesSelected(e: Event) {
  const input = e.target as HTMLInputElement
  if (input.files) {
    addAttachments(Array.from(input.files))
    input.value = ''
  }
}

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function execCommand(cmd: string, value?: string) {
  document.execCommand(cmd, false, value)
  editorRef.value?.focus()
}

function formatBold() { execCommand('bold') }
function formatItalic() { execCommand('italic') }
function formatUnderline() { execCommand('underline') }
function formatList() { execCommand('insertUnorderedList') }

function formatLink() {
  const url = prompt('Enter URL:')
  if (url) execCommand('createLink', url)
}

function onEditorInput() {
  if (editorRef.value) {
    composeBody.value = editorRef.value.innerHTML
  }
}

// Reset editor content when modal opens
watch(isOpen, (open) => {
  if (open && editorRef.value) {
    editorRef.value.innerHTML = composeBody.value || ''
  }
})
</script>

<template>
  <Transition name="overlay">
    <div v-if="isOpen" class="modal-overlay" @click.self="closeCompose">
      <div class="modal-wrap">
        <!-- Ambient glow behind the card -->
        <div class="modal-ambient-glow" />

        <div class="modal-card">
          <!-- Header -->
          <div class="modal-header">
            <div class="modal-title-row">
              <div class="modal-badge">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
                  <path d="M4 20l1.2-4.2L16.4 4.6a1.5 1.5 0 0 1 2.1 0l1 1a1.5 1.5 0 0 1 0 2.1L8.2 18.8 4 20Z" stroke="white" stroke-width="1.7" stroke-linejoin="round"/>
                </svg>
              </div>
              <div class="modal-title">New message</div>
            </div>
            <button class="close-btn" @click="closeCompose">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
              </svg>
            </button>
          </div>

          <!-- Fields -->
          <div class="modal-fields">
            <div class="modal-field" :class="{ focused: false }">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" style="color:#5a6178;flex-shrink:0;">
                <circle cx="12" cy="8" r="3.4" stroke="currentColor" stroke-width="1.6"/>
                <path d="M5 20c.8-3.8 3.5-6 7-6s6.2 2.2 7 6" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
              </svg>
              <input v-model="composeTo" type="text" placeholder="To">
            </div>
            <div class="modal-field">
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" style="color:#5a6178;flex-shrink:0;">
                <path d="M4 6h16M4 12h10M4 18h13" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
              </svg>
              <input v-model="composeSubject" type="text" class="subject-input" placeholder="Subject">
            </div>
          </div>

          <!-- Toolbar -->
          <div class="modal-toolbar">
            <button class="toolbar-btn" title="Bold" @mousedown.prevent="formatBold">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M6 4h7a3.5 3.5 0 0 1 0 7H6zM6 11h8a3.5 3.5 0 0 1 0 7H6z" stroke="currentColor" stroke-width="1.8" stroke-linejoin="round"/>
              </svg>
            </button>
            <button class="toolbar-btn" title="Italic" @mousedown.prevent="formatItalic">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M10 5h7M8 19h7M13 5l-4 14" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
              </svg>
            </button>
            <button class="toolbar-btn" title="Underline" @mousedown.prevent="formatUnderline">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M6 19h12M9 19V6.5a3 3 0 0 1 6 0" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
              </svg>
            </button>
            <div class="toolbar-divider" />
            <button class="toolbar-btn" title="List" @mousedown.prevent="formatList">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M4 6h16M4 12h10M4 18h13" stroke="currentColor" stroke-width="1.7" stroke-linecap="round"/>
              </svg>
            </button>
            <button class="toolbar-btn" title="Link" @mousedown.prevent="formatLink">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M9 12l2 2 4-4" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/>
                <path d="M6 10.5a3.5 3.5 0 0 1 3.5-3.5h.7M18 13.5a3.5 3.5 0 0 1-3.5 3.5h-.7" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
              </svg>
            </button>
            <button class="toolbar-btn" title="Attach" @click="triggerFileInput">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                <path d="M8 12.5l6.5-6.5a3 3 0 0 1 4.2 4.2L10 18.9a5 5 0 1 1-7-7L11.5 3.4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
            </button>
            <input ref="fileInput" type="file" multiple style="display:none" @change="onFilesSelected">
          </div>

          <!-- Attached files -->
          <div v-if="attachments.length" class="attachments-list">
            <div v-for="(file, i) in attachments" :key="i" class="attachment-chip">
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" style="flex-shrink:0;">
                <path d="M8 12.5l6.5-6.5a3 3 0 0 1 4.2 4.2L10 18.9a5 5 0 1 1-7-7L11.5 3.4" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/>
              </svg>
              <span class="attachment-name">{{ file.name }}</span>
              <span class="attachment-size">{{ formatSize(file.size) }}</span>
              <button class="attachment-remove" @click="removeAttachment(i)">
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2.5" stroke-linecap="round"/></svg>
              </button>
            </div>
          </div>

          <!-- Body editor -->
          <div
            ref="editorRef"
            class="modal-body"
            contenteditable="true"
            data-placeholder="Write your message..."
            @input="onEditorInput"
          />

          <!-- Footer -->
          <div class="modal-footer">
            <button class="discard-btn" title="Discard" @click="discardDraft">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
                <path d="M4 7h16M9.5 7V5a1.5 1.5 0 0 1 1.5-1.5h2A1.5 1.5 0 0 1 14.5 5v2M6 7l1 12.5A2 2 0 0 0 9 21.3h6a2 2 0 0 0 2-1.8L18 7" stroke="currentColor" stroke-width="1.6" stroke-linejoin="round"/>
              </svg>
            </button>
            <div class="footer-actions">
              <button class="btn-outline" @click="saveDraft">Save draft</button>
              <button class="btn-send" @click="sendMessage">
                Send
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                  <path d="M3 11L20 4L13 21L11 13L3 11Z" fill="white"/>
                </svg>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(4, 5, 10, 0.65);
  backdrop-filter: blur(6px);
  -webkit-backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 50;
}
.modal-wrap {
  position: relative;
  width: 640px;
  max-width: 92vw;
  max-height: 86vh;
}
.modal-ambient-glow {
  position: absolute;
  top: -40px;
  left: 50%;
  transform: translateX(-50%);
  width: 400px;
  height: 200px;
  border-radius: 50%;
  background: #29A3E8;
  opacity: 0.28;
  filter: blur(80px);
  pointer-events: none;
}
.modal-card {
  position: relative;
  display: flex;
  flex-direction: column;
  max-height: 86vh;
  background: rgba(19, 22, 35, 0.85);
  backdrop-filter: blur(30px) saturate(160%);
  -webkit-backdrop-filter: blur(30px) saturate(160%);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 24px;
  box-shadow: 0 40px 90px -20px rgba(0, 0, 0, 0.7), 0 0 0 1px rgba(255, 255, 255, 0.02) inset;
  overflow: hidden;
  animation: modalIn .35s cubic-bezier(.22,1,.36,1) both;
}
@keyframes modalIn {
  from { opacity: 0; transform: translateY(18px) scale(0.97); }
  to   { opacity: 1; transform: translateY(0) scale(1); }
}
.modal-header {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 22px 16px;
}
.modal-title-row {
  display: flex;
  align-items: center;
  gap: 11px;
}
.modal-badge {
  width: 32px;
  height: 32px;
  border-radius: 10px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  box-shadow: 0 6px 16px rgba(41, 163, 232, 0.35);
}
.modal-title {
  font-size: 15.5px;
  font-weight: 700;
  color: #f0f1f6;
}
.close-btn {
  width: 30px;
  height: 30px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background .15s, color .15s;
}
.close-btn:hover { background: rgba(255,255,255,.06); color: #e6e8f0; }

.modal-fields {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 0 22px 18px;
}
.modal-field {
  display: flex;
  align-items: center;
  gap: 10px;
  height: 44px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.035);
  border: 1px solid rgba(255, 255, 255, 0.08);
  padding: 0 14px;
  transition: border-color .15s, box-shadow .15s;
}
.modal-field:focus-within {
  border-color: #29A3E8;
  box-shadow: 0 0 0 3px rgba(139, 92, 246, 0.22);
}
.modal-field input {
  flex: 1;
  border: none;
  background: transparent;
  color: #eceef4;
  font-size: 13.5px;
  font-family: inherit;
  outline: none;
}
.modal-field input::placeholder { color: #5a6178; }
.subject-input { font-weight: 600; }

.modal-toolbar {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 2px;
  margin: 0 22px 14px;
  padding: 6px;
  border-radius: 12px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.06);
  width: fit-content;
}
.toolbar-btn {
  width: 30px;
  height: 30px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background .15s, color .15s;
}
.toolbar-btn:hover { background: rgba(255,255,255,.06); color: #e6e8f0; }
.toolbar-divider {
  width: 1px;
  height: 16px;
  background: rgba(255, 255, 255, 0.1);
  margin: 0 5px;
}
.attachments-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 0 22px 14px;
}
.attachment-chip {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border-radius: 8px;
  background: rgba(255,255,255,0.05);
  border: 1px solid rgba(255,255,255,0.08);
  color: #c3c7d4;
  font-size: 12px;
}
.attachment-name {
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.attachment-size { color: #5f6579; }
.attachment-remove {
  width: 16px;
  height: 16px;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  background: transparent;
  border: none;
  cursor: pointer;
}
.attachment-remove:hover { color: #f36565; }

.modal-body {
  flex: 1;
  min-height: 170px;
  border: none;
  background: transparent;
  color: #d7dae2;
  font-size: 14px;
  font-family: inherit;
  line-height: 1.7;
  outline: none;
  padding: 0 22px 18px;
  width: 100%;
  overflow-y: auto;
}
.modal-body:empty::before {
  content: attr(data-placeholder);
  color: #5a6178;
  pointer-events: none;
}
.modal-body :deep(ul), .modal-body :deep(ol) {
  padding-left: 20px;
  margin: 4px 0;
}
.modal-body :deep(a) {
  color: #29A3E8;
  text-decoration: underline;
}
.modal-footer {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 22px;
  background: rgba(255, 255, 255, 0.02);
  border-top: 1px solid rgba(255, 255, 255, 0.06);
}
.footer-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}
.discard-btn {
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
  transition: color .15s;
}
.discard-btn:hover { color: #f36565; }
.btn-outline {
  height: 40px;
  padding: 0 18px;
  border-radius: 11px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.04);
  color: #e6e8f0;
  font-size: 13.5px;
  font-weight: 700;
  display: flex;
  align-items: center;
  cursor: pointer;
  font-family: inherit;
  transition: background .15s;
}
.btn-outline:hover { background: rgba(255,255,255,.08); }
.btn-send {
  height: 40px;
  padding: 0 22px;
  border-radius: 11px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
  font-size: 13.5px;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  box-shadow: 0 10px 24px -8px rgba(41, 163, 232, 0.35);
  transition: transform .18s;
  border: none;
  font-family: inherit;
}
.btn-send:hover { transform: translateY(-2px); }
.btn-send:active { transform: translateY(0); }

/* Overlay transition */
.overlay-enter-active { animation: fadeIn .2s ease both; }
.overlay-leave-active { transition: opacity .2s ease; }
.overlay-leave-to { opacity: 0; }
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
</style>
