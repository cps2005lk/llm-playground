<template>
  <aside class="params-panel">
    <h2>Parameters</h2>

    <div class="field">
      <label>Model</label>
      <select v-model="store.params.model">
        <option v-for="m in store.models" :key="m.name" :value="m.name">{{ m.name }}</option>
        <option v-if="!store.models.length" disabled value="">No models found</option>
      </select>
    </div>

    <div class="field">
      <label>Temperature <span class="value">{{ store.params.temperature.toFixed(2) }}</span></label>
      <input type="range" min="0" max="2" step="0.01" v-model.number="store.params.temperature" />
    </div>

    <div class="field">
      <label>Max Tokens <span class="value">{{ store.params.maxTokens }}</span></label>
      <input type="range" min="1" max="4096" step="1" v-model.number="store.params.maxTokens" />
    </div>

    <div class="field">
      <label>Top-P <span class="value">{{ store.params.topP.toFixed(2) }}</span></label>
      <input type="range" min="0" max="1" step="0.01" v-model.number="store.params.topP" />
    </div>

    <div class="field">
      <label>Top-K <span class="value">{{ store.params.topK }}</span></label>
      <input type="range" min="1" max="100" step="1" v-model.number="store.params.topK" />
    </div>
  </aside>
</template>

<script setup>
import { usePlaygroundStore } from '../stores/playground'
const store = usePlaygroundStore()
</script>

<style scoped>
.params-panel {
  background: #1e1e2e;
  border-right: 1px solid #313244;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  min-width: 220px;
  max-width: 260px;
}

h2 {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: #6c7086;
  margin: 0;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}

label {
  font-size: 0.8rem;
  color: #cdd6f4;
  display: flex;
  justify-content: space-between;
}

.value {
  color: #89b4fa;
  font-variant-numeric: tabular-nums;
}

input[type="range"] {
  width: 100%;
  accent-color: #89b4fa;
  cursor: pointer;
}

select {
  background: #313244;
  color: #cdd6f4;
  border: 1px solid #45475a;
  border-radius: 6px;
  padding: 0.35rem 0.5rem;
  font-size: 0.85rem;
  cursor: pointer;
  width: 100%;
}
</style>
