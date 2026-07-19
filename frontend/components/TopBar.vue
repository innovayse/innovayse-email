<script setup lang="ts">
const now = new Date()

const dateStr = computed(() => {
  return now.toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
})

// e.g. "Wednesday · July 8, 2026"
const [weekday, ...rest] = dateStr.value.split(', ')
const dayDate = rest.join(', ')
</script>

<template>
  <div class="topbar">
    <div class="topbar-date">
      <span>{{ weekday }}</span>
      <span style="color:#4a4f5e;">·</span>
      <span>{{ dayDate }}</span>
    </div>

    <div class="topbar-icons">
      <div id="inno-launcher-mount"></div>
      <div id="inno-account-mount"></div>
      <!-- Calendar -->
      <button class="icon-btn" title="Calendar">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
          <rect x="3" y="4.5" width="18" height="16" rx="3" stroke="currentColor" stroke-width="1.6"/>
          <path d="M3 9.5h18M8 3v3M16 3v3" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
        </svg>
      </button>

      <!-- Contacts -->
      <button class="icon-btn" title="Contacts">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
          <circle cx="9" cy="8" r="3" stroke="currentColor" stroke-width="1.6"/>
          <path d="M3.5 20c.7-3.6 3-5.5 5.5-5.5s4.8 1.9 5.5 5.5" stroke="currentColor" stroke-width="1.6" stroke-linecap="round"/>
          <circle cx="17.5" cy="9" r="2.2" stroke="currentColor" stroke-width="1.5"/>
          <path d="M15 20c.4-2.3 1.6-3.8 3-4.3" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/>
        </svg>
      </button>

      <!-- Settings -->
      <button class="icon-btn" title="Settings">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="2.8" stroke="currentColor" stroke-width="1.6"/>
          <path d="M19.4 13.5c.1-.5.1-1 0-1.5l1.9-1.5-2-3.4-2.3.7c-.4-.3-.8-.6-1.3-.8L15.3 4h-4l-.4 2.9c-.5.2-.9.5-1.3.8l-2.3-.7-2 3.4L7.2 12c-.1.5-.1 1 0 1.5l-1.9 1.5 2 3.4 2.3-.7c.4.3.8.6 1.3.8l.4 3h4l.4-2.9c.5-.2.9-.5 1.3-.8l2.3.7 2-3.4-1.9-1.6Z" stroke="currentColor" stroke-width="1.3" stroke-linejoin="round"/>
        </svg>
      </button>

      <!-- History (accent) -->
      <button class="icon-btn accent" title="Activity">
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="9" stroke="white" stroke-width="1.6"/>
          <path d="M12 8v5l3 2" stroke="white" stroke-width="1.6" stroke-linecap="round"/>
        </svg>
      </button>
    </div>
  </div>
</template>

<style scoped>
.topbar {
  flex-shrink: 0;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  background: rgba(12, 14, 22, 0.4);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-bottom: 1px solid rgba(255, 255, 255, 0.06);
  position: relative;
  z-index: 1;
}
.topbar-date {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #8890a3;
  font-size: 12.5px;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}
.topbar-icons {
  display: flex;
  align-items: center;
  gap: 6px;
}
.icon-btn {
  width: 34px;
  height: 34px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #8890a3;
  cursor: pointer;
  background: transparent;
  border: none;
  font-family: inherit;
  transition: background .15s, color .15s;
}
.icon-btn:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #e6e8f0;
}
.icon-btn.accent {
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
  box-shadow: 0 4px 14px rgba(41, 163, 232, 0.35);
}
.icon-btn.accent:hover {
  background: linear-gradient(135deg, #29A3E8, #8B5CF6);
  color: white;
}
</style>
