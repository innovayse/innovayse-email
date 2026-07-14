<script setup lang="ts">
// Entry point: check auth → check mailbox selection → redirect to /mail
// In production, auth is via SSO JWT (cookie). Here we check for a valid token
// by probing the /api/mailboxes endpoint; redirect to SSO login on 401.

const { activeMailbox, fetchMailboxes } = useMailbox()

const SSO_LOGIN_URL = '/auth/login'

// Check auth cookie first — if not present, redirect to SSO immediately
const authedCookie = useCookie('authed')

onMounted(async () => {
  if (!authedCookie.value) {
    window.location.href = SSO_LOGIN_URL
    return
  }

  try {
    const mailboxes = await fetchMailboxes()

    if (mailboxes.length === 0) {
      await navigateTo('/select-mailbox')
      return
    }

    if (!activeMailbox.value) {
      if (mailboxes.length === 1) {
        activeMailbox.value = mailboxes[0]
        await navigateTo('/mail')
      } else {
        await navigateTo('/select-mailbox')
      }
    } else {
      await navigateTo('/mail')
    }
  } catch (e: any) {
    const status = e?.status || e?.statusCode || e?.response?.status
    if (status === 401) {
      window.location.href = SSO_LOGIN_URL
    } else {
      await navigateTo('/select-mailbox')
    }
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
