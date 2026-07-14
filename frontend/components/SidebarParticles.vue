<script setup lang="ts">
// Non-interactive drifting particle canvas.
// Small dots in the two accent colors, connecting lines between nearby particles.
// Continuous slow drift — NO mouse interaction (unlike SSO ParticleCanvas).
// Ported from reference.html #sidebar-particles script block.

const canvasRef = ref<HTMLCanvasElement | null>(null)

const C1 = { r: 41,  g: 163, b: 232 } // #29A3E8
const C2 = { r: 139, g: 92,  b: 246 } // #8B5CF6

interface Particle {
  x: number
  y: number
  vx: number
  vy: number
  r: number
}

onMounted(() => {
  const canvas = canvasRef.value
  if (!canvas) return

  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const dpr = Math.min(window.devicePixelRatio || 1, 2)
  let width = 0
  let height = 0
  let particles: Particle[] = []
  let animId: number

  function resize() {
    const parent = canvas!.parentElement
    if (!parent) return
    const rect = parent.getBoundingClientRect()
    width  = rect.width
    height = rect.height
    canvas!.width  = width  * dpr
    canvas!.height = height * dpr
    canvas!.style.width  = width  + 'px'
    canvas!.style.height = height + 'px'
    ctx!.setTransform(dpr, 0, 0, dpr, 0, 0)

    const area  = width * height
    const count = Math.max(16, Math.min(Math.round(area / 4500), 40))
    particles = Array.from({ length: count }, () => ({
      x:  Math.random() * width,
      y:  Math.random() * height,
      vx: (Math.random() - 0.5) * 0.16,
      vy: (Math.random() - 0.5) * 0.16,
      r:  Math.random() * 1.4 + 0.8,
    }))
  }

  function tick() {
    animId = requestAnimationFrame(tick)
    if (!width) return

    ctx!.clearRect(0, 0, width, height)

    // Move particles + wrap edges
    for (const p of particles) {
      p.x += p.vx; p.y += p.vy
      if (p.x < -8)          p.x = width  + 8
      if (p.x > width  + 8)  p.x = -8
      if (p.y < -8)          p.y = height + 8
      if (p.y > height + 8)  p.y = -8
    }

    // Draw connecting lines between nearby particles
    for (let i = 0; i < particles.length; i++) {
      for (let j = i + 1; j < particles.length; j++) {
        const a = particles[i], b = particles[j]
        const dx = a.x - b.x, dy = a.y - b.y
        const d2 = dx * dx + dy * dy
        if (d2 < 95 * 95) {
          const alpha = (1 - Math.sqrt(d2) / 95) * 0.14
          ctx!.strokeStyle = `rgba(${C1.r},${C1.g},${C1.b},${alpha})`
          ctx!.lineWidth = 1
          ctx!.beginPath()
          ctx!.moveTo(a.x, a.y)
          ctx!.lineTo(b.x, b.y)
          ctx!.stroke()
        }
      }
    }

    // Draw dots
    particles.forEach((p, idx) => {
      const c = idx % 2 === 0 ? C1 : C2
      ctx!.beginPath()
      ctx!.fillStyle   = `rgba(${c.r},${c.g},${c.b},0.7)`
      ctx!.shadowColor = `rgba(${c.r},${c.g},${c.b},0.8)`
      ctx!.shadowBlur  = 5
      ctx!.arc(p.x, p.y, p.r, 0, Math.PI * 2)
      ctx!.fill()
    })
    ctx!.shadowBlur = 0
  }

  const ro = new ResizeObserver(resize)
  ro.observe(canvas.parentElement!)
  resize()
  tick()

  onUnmounted(() => {
    cancelAnimationFrame(animId)
    ro.disconnect()
  })
})
</script>

<template>
  <canvas
    ref="canvasRef"
    style="position:absolute;inset:0;width:100%;height:100%;pointer-events:none;z-index:0;"
  />
</template>
