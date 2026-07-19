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

    <div class="divider" />

    <h2>Connection</h2>

    <div class="preset-row">
      <button
        class="preset-btn"
        :class="{ active: isLocalPreset }"
        @click="applyPreset('local')"
      >Local</button>
      <button
        class="preset-btn"
        :class="{ active: isCloudPreset }"
        @click="applyPreset('cloud')"
      >Cloud</button>
    </div>

    <div class="field">
      <label>Base URL</label>
      <input type="text" v-model="connBaseUrl" placeholder="http://localhost:11434" spellcheck="false" />
    </div>

    <div class="field">
      <label>API Key</label>
      <input type="password" v-model="connApiKey" placeholder="Leave blank for local" autocomplete="off" />
    </div>

    <button class="apply-btn" :disabled="applying" @click="applyConnection">
      {{ applying ? 'Applying…' : 'Apply' }}
    </button>

    <p v-if="connStatus" class="conn-status" :class="connStatusClass">{{ connStatus }}</p>
  </aside>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { usePlaygroundStore } from '../stores/playground'
import { fetchProviderConfig, updateProviderConfig, fetchModels } from '../services/api'

const store = usePlaygroundStore()

const LOCAL_URL = 'http://localhost:11434'
const CLOUD_URL = 'https://ollama.com'

const connBaseUrl = ref(LOCAL_URL)
const connApiKey = ref('')
const applying = ref(false)
const connStatus = ref('')
const connStatusClass = ref('')

const isLocalPreset = computed(() => connBaseUrl.value === LOCAL_URL && connApiKey.value === '')
const isCloudPreset = computed(() => connBaseUrl.value === CLOUD_URL)

onMounted(async () => {
  try {
    const config = await fetchProviderConfig()
    connBaseUrl.value = config.baseUrl
    if (!config.hasApiKey) connApiKey.value = ''
  } catch {
    // backend may not be running yet; defaults are fine
  }
})

function applyPreset(mode) {
  if (mode === 'local') {
    connBaseUrl.value = LOCAL_URL
    connApiKey.value = ''
  } else {
    connBaseUrl.value = CLOUD_URL
  }
}

async function applyConnection() {
  if (!connBaseUrl.value.trim()) return
  applying.value = true
  connStatus.value = ''
  try {
    await updateProviderConfig(connBaseUrl.value.trim(), connApiKey.value)
    // Refresh model list immediately after switching
    const models = await fetchModels()
    store.models = models
    if (models.length && !models.find(m => m.name === store.params.model)) {
      store.params.model = models[0].name
    }
    connStatus.value = `Connected — ${models.length} model${models.length !== 1 ? 's' : ''} found`
    connStatusClass.value = 'ok'
  } catch (err) {
    connStatus.value = err?.response?.data || 'Failed to connect'
    connStatusClass.value = 'err'
  } finally {
    applying.value = false
  }
}
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

.divider {
  border-top: 1px solid #313244;
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

select,
input[type="text"],
input[type="password"] {
  background: #313244;
  color: #cdd6f4;
  border: 1px solid #45475a;
  border-radius: 6px;
  padding: 0.35rem 0.5rem;
  font-size: 0.85rem;
  width: 100%;
  box-sizing: border-box;
}

select { cursor: pointer; }

input[type="text"]:focus,
input[type="password"]:focus {
  outline: none;
  border-color: #89b4fa;
}

.preset-row {
  display: flex;
  gap: 0.5rem;
}

.preset-btn {
  flex: 1;
  background: #313244;
  color: #cdd6f4;
  border: 1px solid #45475a;
  border-radius: 6px;
  padding: 0.3rem 0;
  font-size: 0.8rem;
  cursor: pointer;
}

.preset-btn.active {
  background: #1e3a5f;
  border-color: #89b4fa;
  color: #89b4fa;
}

.apply-btn {
  background: #89b4fa;
  color: #1e1e2e;
  border: none;
  border-radius: 6px;
  padding: 0.45rem;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  width: 100%;
}

.apply-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.conn-status {
  font-size: 0.75rem;
  margin: 0;
  padding: 0.35rem 0.5rem;
  border-radius: 4px;
}

.conn-status.ok {
  background: #1a2e1a;
  color: #a6e3a1;
}

.conn-status.err {
  background: #2e1a1a;
  color: #f38ba8;
}
</style>
