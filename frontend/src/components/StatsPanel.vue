<template>
  <div class="stats-panel" v-if="store.response || store.loading">

    <!-- Token Usage -->
    <section class="stat-section">
      <h3>Token Usage</h3>
      <div class="stat-grid">
        <div class="stat-card">
          <span class="stat-label">Input</span>
          <span class="stat-value">{{ usage.inputTokens ?? '—' }}</span>
          <span class="stat-sub">tokens sent</span>
        </div>
        <div class="stat-card">
          <span class="stat-label">Output</span>
          <span class="stat-value">{{ usage.outputTokens ?? '—' }}</span>
          <span class="stat-sub">tokens received</span>
        </div>
        <div class="stat-card">
          <span class="stat-label">Total</span>
          <span class="stat-value highlight">{{ usage.totalTokens ?? '—' }}</span>
          <span class="stat-sub">this request</span>
        </div>
        <div class="stat-card">
          <span class="stat-label">Speed</span>
          <span class="stat-value">{{ usage.tokensPerSecond ?? '—' }}</span>
          <span class="stat-sub">tokens / sec</span>
        </div>
      </div>
      <div class="stat-card wide">
        <span class="stat-label">Request time</span>
        <span class="stat-value">{{ usage.durationMs ? (usage.durationMs / 1000).toFixed(2) + 's' : '—' }}</span>
      </div>
    </section>

    <!-- Context Window -->
    <section class="stat-section" v-if="store.contextMaxTokens > 0">
      <h3>Context Window</h3>
      <div class="context-info">
        <span>{{ store.contextUsedTokens.toLocaleString() }}</span>
        <span class="context-sep">of</span>
        <span>{{ store.contextMaxTokens.toLocaleString() }}</span>
        <span class="context-sep">tokens used</span>
        <span class="context-pct" :style="{ color: contextPctColor }">
          ({{ contextPct }}%)
        </span>
      </div>
      <div class="progress-track">
        <div
          class="progress-fill"
          :style="{ width: contextPct + '%', background: contextPctColor }"
        ></div>
      </div>
      <p class="context-explain">
        The context window is the model's working memory — everything it can "see" at once.
        Once full, older content gets dropped.
      </p>
    </section>

    <!-- Raw Request -->
    <section class="stat-section">
      <h3>
        Raw Request
        <button class="toggle-btn" @click="showRaw = !showRaw">
          {{ showRaw ? 'Hide' : 'Show' }}
        </button>
      </h3>
      <pre v-if="showRaw" class="raw-block">{{ prettyRequest }}</pre>
      <p v-else class="context-explain">
        The exact JSON payload sent to the backend on your last request.
      </p>
    </section>

  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { usePlaygroundStore } from '../stores/playground'

const store = usePlaygroundStore()
const showRaw = ref(false)

const usage = computed(() => store.response?.usage ?? {})

const contextPct = computed(() => {
  if (!store.contextMaxTokens) return 0
  return Math.min(100, ((store.contextUsedTokens / store.contextMaxTokens) * 100).toFixed(1))
})

const contextPctColor = computed(() => {
  const p = contextPct.value
  if (p < 50) return 'hsl(120, 60%, 55%)'
  if (p < 80) return 'hsl(45, 90%, 55%)'
  return 'hsl(0, 70%, 60%)'
})

const prettyRequest = computed(() =>
  store.lastRequest ? JSON.stringify(store.lastRequest, null, 2) : ''
)
</script>

<style scoped>
.stats-panel {
  background: #1e1e2e;
  border: 1px solid #313244;
  border-radius: 8px;
  padding: 1.25rem 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.stat-section h3 {
  font-size: 0.72rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: #6c7086;
  margin: 0 0 0.75rem;
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.stat-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.stat-card {
  background: #181825;
  border: 1px solid #313244;
  border-radius: 6px;
  padding: 0.6rem 0.75rem;
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.stat-card.wide {
  grid-column: 1 / -1;
  flex-direction: row;
  justify-content: space-between;
  align-items: center;
}

.stat-label {
  font-size: 0.7rem;
  color: #6c7086;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.stat-value {
  font-size: 1.3rem;
  font-weight: 600;
  color: #cdd6f4;
  font-variant-numeric: tabular-nums;
}

.stat-value.highlight {
  color: #89b4fa;
}

.stat-sub {
  font-size: 0.68rem;
  color: #45475a;
}

/* Context window */
.context-info {
  display: flex;
  align-items: baseline;
  gap: 0.4rem;
  font-size: 0.9rem;
  color: #cdd6f4;
  margin-bottom: 0.5rem;
  font-variant-numeric: tabular-nums;
}

.context-sep { color: #6c7086; font-size: 0.8rem; }

.context-pct { font-weight: 600; }

.progress-track {
  background: #313244;
  border-radius: 4px;
  height: 8px;
  overflow: hidden;
  margin-bottom: 0.6rem;
}

.progress-fill {
  height: 100%;
  border-radius: 4px;
  transition: width 0.4s ease, background 0.4s ease;
  min-width: 2px;
}

.context-explain {
  font-size: 0.75rem;
  color: #585b70;
  line-height: 1.5;
  margin: 0;
}

/* Raw request */
.toggle-btn {
  background: #313244;
  border: none;
  color: #89b4fa;
  font-size: 0.7rem;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
  cursor: pointer;
  text-transform: none;
  letter-spacing: 0;
}

.toggle-btn:hover { background: #45475a; }

.raw-block {
  background: #181825;
  border: 1px solid #313244;
  border-radius: 6px;
  padding: 0.75rem 1rem;
  font-family: monospace;
  font-size: 0.78rem;
  color: #a6e3a1;
  overflow-x: auto;
  margin: 0;
  white-space: pre;
}
</style>
