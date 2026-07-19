<script setup lang="ts">
const email = ref('')
const password = ref('')
const errorMessage = ref('')
const submitting = ref(false)

async function handleSubmit() {
  errorMessage.value = ''
  submitting.value = true
  try {
    await $fetch('/api/auth/login', {
      method: 'POST',
      body: { email: email.value, password: password.value },
    })
    await navigateTo('/')
  } catch (e: any) {
    const status = e?.status || e?.statusCode || e?.response?.status
    errorMessage.value = status === 503
      ? 'Mail server is unavailable. Please try again shortly.'
      : 'Invalid email or password.'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="login-screen">
    <form class="login-card" @submit.prevent="handleSubmit">
      <div class="login-mark">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
          <path d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z" fill="white" fill-opacity="0.95"/>
        </svg>
      </div>
      <h1>Innovayse Mail</h1>
      <label>
        <span>Email</span>
        <input v-model="email" type="email" required autocomplete="username" />
      </label>
      <label>
        <span>Password</span>
        <input v-model="password" type="password" required autocomplete="current-password" />
      </label>
      <p v-if="errorMessage" class="login-error">{{ errorMessage }}</p>
      <button type="submit" :disabled="submitting">{{ submitting ? 'Signing in…' : 'Sign in' }}</button>
    </form>
  </div>
</template>

<style scoped>
.login-screen {
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 100vh;
  background: #07080d;
}
.login-card {
  width: 320px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 32px;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
}
.login-mark {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 4px;
}
.login-card h1 {
  color: white;
  font-size: 18px;
  margin: 0 0 8px;
}
.login-card label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
  color: rgba(255, 255, 255, 0.7);
}
.login-card input {
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: rgba(255, 255, 255, 0.03);
  color: white;
  font-size: 14px;
}
.login-error {
  color: #f87171;
  font-size: 13px;
  margin: 0;
}
.login-card button {
  margin-top: 4px;
  padding: 10px 12px;
  border-radius: 8px;
  border: none;
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
  font-weight: 600;
  cursor: pointer;
}
.login-card button:disabled {
  opacity: 0.6;
  cursor: default;
}
</style>
