<script setup lang="ts">
useHead({
  htmlAttrs: { class: 'dark' },
  link: [
    { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
    { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
    {
      rel: 'stylesheet',
      href: 'https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600;700;800&display=swap',
    },
  ],
})
</script>

<template>
  <div class="min-h-screen bg-[#07080d] font-sans">
    <NuxtPage />
  </div>
</template>
