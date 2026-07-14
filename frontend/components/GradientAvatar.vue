<script setup lang="ts">
const props = defineProps<{
  name: string
  gradient?: string
  size?: 'sm' | 'md' | 'lg'
}>()

const initials = computed(() => {
  const parts = props.name.trim().split(/\s+/)
  if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase()
  return props.name.slice(0, 2).toUpperCase()
})

// Deterministic gradient from name if not provided
const GRADIENTS = [
  'linear-gradient(135deg,#29A3E8,#8B5CF6)',
  'linear-gradient(135deg,#635BFF,#8B5CF6)',
  'linear-gradient(135deg,#22D3C9,#3B82F6)',
  'linear-gradient(135deg,#5B5FEF,#8B5CF6)',
  'linear-gradient(135deg,#EF4444,#8B5CF6)',
  'linear-gradient(135deg,#EC4899,#8B5CF6)',
  'linear-gradient(135deg,#29A3E8,#22D3C9)',
  'linear-gradient(135deg,#6b7280,#3B82F6)',
]

const resolvedGradient = computed(() => {
  if (props.gradient) return props.gradient
  let hash = 0
  for (let i = 0; i < props.name.length; i++) {
    hash = (hash * 31 + props.name.charCodeAt(i)) >>> 0
  }
  return GRADIENTS[hash % GRADIENTS.length]
})

const sizeClass = computed(() => {
  switch (props.size) {
    case 'sm': return 'w-[38px] h-[38px] rounded-[11px] text-[12.5px]'
    case 'lg': return 'w-[44px] h-[44px] rounded-[12px] text-[14px]'
    default: return 'w-[38px] h-[38px] rounded-[11px] text-[12.5px]'
  }
})
</script>

<template>
  <div
    class="flex items-center justify-center flex-shrink-0 font-bold text-white"
    :class="sizeClass"
    :style="{ background: resolvedGradient }"
  >
    {{ initials }}
  </div>
</template>
