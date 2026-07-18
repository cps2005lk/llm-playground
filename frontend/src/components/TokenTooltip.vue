<template>
  <div class="tooltip" :style="{ left: x + 'px', top: y + 'px' }">
    <div class="tooltip-title">Alternatives</div>
    <div v-for="alt in alternatives" :key="alt.text" class="alt-row">
      <span class="alt-text">{{ displayText(alt.text) }}</span>
      <div class="alt-bar-wrap">
        <div class="alt-bar" :style="{ width: (alt.probability * 100).toFixed(1) + '%', background: probColor(alt.probability) }"></div>
      </div>
      <span class="alt-prob">{{ (alt.probability * 100).toFixed(1) }}%</span>
    </div>
  </div>
</template>

<script setup>
defineProps({
  alternatives: { type: Array, required: true },
  x: { type: Number, required: true },
  y: { type: Number, required: true }
})

function displayText(text) {
  return text.replace(/\n/g, '↵').replace(/\t/g, '→') || '(empty)'
}

function probColor(p) {
  // green hue=120 at p=1, red hue=0 at p=0
  const hue = Math.round(p * 120)
  return `hsl(${hue}, 70%, 55%)`
}
</script>

<style scoped>
.tooltip {
  position: fixed;
  z-index: 1000;
  background: #1e1e2e;
  border: 1px solid #45475a;
  border-radius: 8px;
  padding: 0.6rem 0.75rem;
  min-width: 200px;
  max-width: 280px;
  box-shadow: 0 8px 24px rgba(0,0,0,0.5);
  pointer-events: none;
}

.tooltip-title {
  font-size: 0.7rem;
  text-transform: uppercase;
  letter-spacing: 0.07em;
  color: #6c7086;
  margin-bottom: 0.5rem;
}

.alt-row {
  display: grid;
  grid-template-columns: 80px 1fr 44px;
  align-items: center;
  gap: 0.4rem;
  margin-bottom: 0.3rem;
}

.alt-text {
  font-family: monospace;
  font-size: 0.82rem;
  color: #cdd6f4;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.alt-bar-wrap {
  background: #313244;
  border-radius: 3px;
  height: 6px;
  overflow: hidden;
}

.alt-bar {
  height: 100%;
  border-radius: 3px;
  transition: width 0.15s ease;
}

.alt-prob {
  font-size: 0.75rem;
  color: #a6adc8;
  text-align: right;
  font-variant-numeric: tabular-nums;
}
</style>
