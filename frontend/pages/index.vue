<script setup lang="ts">
// Entry point: check for an active mailbox session → /mail, else → /login

const { fetchActiveMailbox } = useMailbox()

onMounted(async () => {
  try {
    await fetchActiveMailbox()
    await navigateTo('/mail')
  } catch {
    await navigateTo('/login')
  }
})
</script>

<template>
  <!-- Loading splash while redirect resolves -->
  <div class="splash">
    <div class="splash-mark">
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none">
        <path d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z" fill="white" fill-opacity="0.95"/>
      </svg>
    </div>
  </div>
</template>

<style scoped>
.splash {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100vh;
  background: #07080d;
}
.splash-mark {
  width: 64px;
  height: 64px;
  border-radius: 18px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 16px 48px rgba(41, 163, 232, 0.4);
  animation: pulse 2s ease-in-out infinite;
}
@keyframes pulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.06); opacity: 0.85; }
}
</style>
