<template>
  <div class="response-display">
    <div v-if="!response" class="placeholder">
      Response will appear here…
    </div>

    <!-- Plain text when logprobs aren't available -->
    <div v-else-if="!response.logprobsAvailable" class="plain-text">
      {{ response.text }}
    </div>

    <!-- Token-highlighted view -->
    <div v-else class="token-view" @mouseleave="hideTooltip">
      <span
        v-for="(token, i) in response.tokens"
        :key="i"
        class="token"
        :style="{ background: tokenBg(token.probability) }"
        @mouseenter="showTooltip($event, token)"
        @mouseleave="hideTooltip"
      >{{ token.text }}</span>
    </div>

    <TokenTooltip
      v-if="tooltip.visible"
      :alternatives="tooltip.alternatives"
      :x="tooltip.x"
      :y="tooltip.y"
    />

    <div v-if="response && !response.logprobsAvailable" class="logprob-notice">
      ⚠ This model did not return token probabilities. Highlighting is unavailable.
    </div>
  </div>
</template>

<script setup>
import { reactive } from 'vue'
import TokenTooltip from './TokenTooltip.vue'

defineProps({
  response: { type: Object, default: null }
})

const tooltip = reactive({ visible: false, alternatives: [], x: 0, y: 0 })

function tokenBg(p) {
  if (p == null) return 'transparent'
  const hue = Math.round(p * 120)
  return `hsla(${hue}, 70%, 55%, 0.25)`
}

function showTooltip(event, token) {
  if (!token.alternatives?.length) return
  const rect = event.target.getBoundingClientRect()
  tooltip.alternatives = token.alternatives
  tooltip.x = rect.left
  tooltip.y = rect.bottom + 6
  tooltip.visible = true
}

function hideTooltip() {
  tooltip.visible = false
}
</script>

<style scoped>
.response-display {
  flex: 1;
  background: #181825;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  min-height: 200px;
  position: relative;
  line-height: 1.75;
  font-size: 0.95rem;
}

.placeholder {
  color: #45475a;
  font-style: italic;
}

.plain-text {
  color: #cdd6f4;
  white-space: pre-wrap;
  word-break: break-word;
}

.token-view {
  color: #cdd6f4;
  white-space: pre-wrap;
  word-break: break-word;
}

.token {
  border-radius: 3px;
  cursor: default;
  transition: background 0.1s ease;
  padding: 1px 0;
}

.token:hover {
  outline: 1px solid rgba(137, 180, 250, 0.5);
}

.logprob-notice {
  margin-top: 1rem;
  font-size: 0.78rem;
  color: #f38ba8;
}
</style>
