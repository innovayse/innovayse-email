<script setup lang="ts">
const { mailboxes, activeMailbox, fetchMailboxes, selectMailbox } = useMailbox()

onMounted(async () => {
  await fetchMailboxes()
})

async function pick(email: string) {
  selectMailbox(email)
  await navigateTo('/mail')
}
</script>

<template>
  <div class="page">
    <!-- Background glows -->
    <div class="dot-grid" />
    <div class="glow glow-tl" />
    <div class="glow glow-br" />
    <div class="glow glow-tr1" />
    <div class="glow glow-tr2" />

    <div class="content">
      <!-- Brand mark -->
      <div class="brand-row">
        <div class="brand-mark">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none">
            <path d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z" fill="white" fill-opacity="0.95"/>
          </svg>
        </div>
        <div class="brand-name">Innovayse Mail</div>
      </div>

      <h1 class="heading">Choose a mailbox</h1>
      <p class="sub">Select the account you want to open.</p>

      <div class="cards">
        <MailboxPicker
          v-for="mb in mailboxes"
          :key="mb.email"
          :mailbox="mb"
          @select="pick"
        />

        <div v-if="mailboxes.length === 0" class="empty">
          <p>No mailboxes found for your account.</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page {
  position: relative;
  min-height: 100vh;
  background: #07080d;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
}
.content {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 520px;
  padding: 40px 24px;
}
.brand-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 36px;
}
.brand-mark {
  width: 46px;
  height: 46px;
  border-radius: 14px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 8px 24px rgba(41, 163, 232, 0.35);
}
.brand-name {
  font-size: 20px;
  font-weight: 800;
  color: #f3f4f8;
  letter-spacing: -0.01em;
}
.heading {
  font-size: 28px;
  font-weight: 800;
  color: #f5f6fa;
  letter-spacing: -0.02em;
  margin: 0 0 8px;
}
.sub {
  font-size: 15px;
  color: #8890a3;
  margin: 0 0 28px;
}
.cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.empty {
  text-align: center;
  color: #565d70;
  font-size: 14px;
  padding: 40px 0;
}
</style>
