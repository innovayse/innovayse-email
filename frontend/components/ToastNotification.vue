<script setup lang="ts">
defineProps<{
  message: string
  visible: boolean
}>()
</script>

<template>
  <Transition name="toast">
    <div
      v-if="visible"
      class="toast-wrap"
    >
      <!-- check icon -->
      <div class="toast-icon">
        <svg width="11" height="11" viewBox="0 0 24 24" fill="none">
          <path d="M4 12.5L9.5 18L20 6" stroke="white" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
      </div>
      <span>{{ message }}</span>
    </div>
  </Transition>
</template>

<style scoped>
.toast-wrap {
  position: fixed;
  bottom: 28px;
  left: 50%;
  transform: translateX(-50%);
  background: rgba(20, 23, 36, 0.92);
  backdrop-filter: blur(16px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #eceef4;
  font-size: 13.5px;
  font-weight: 600;
  padding: 12px 20px;
  border-radius: 12px;
  box-shadow: 0 20px 50px -12px rgba(0, 0, 0, 0.6);
  z-index: 60;
  display: flex;
  align-items: center;
  gap: 10px;
  white-space: nowrap;
}

.toast-icon {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

/* Transition */
.toast-enter-active {
  animation: toastIn .25s ease both;
}
.toast-leave-active {
  transition: opacity .2s ease;
}
.toast-leave-to {
  opacity: 0;
}

@keyframes toastIn {
  from { opacity: 0; transform: translateY(10px) translateX(-50%); }
  to   { opacity: 1; transform: translateY(0) translateX(-50%); }
}
</style>
