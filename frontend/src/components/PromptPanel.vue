<template>
  <div class="prompt-panel">
    <textarea
      v-model="prompt"
      class="prompt-input"
      placeholder="Enter your prompt here…"
      rows="5"
      @keydown.ctrl.enter.prevent="handleSubmit"
    ></textarea>
    <div class="prompt-footer">
      <span class="hint">Ctrl+Enter to submit</span>
      <button class="submit-btn" :disabled="store.loading || !prompt.trim()" @click="handleSubmit">
        <span v-if="store.loading" class="spinner"></span>
        <span v-else>Submit</span>
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { usePlaygroundStore } from '../stores/playground'

const store = usePlaygroundStore()
const prompt = ref('')

async function handleSubmit() {
  if (!prompt.value.trim() || store.loading) return
  await store.submit(prompt.value)
}
</script>

<style scoped>
.prompt-panel {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.prompt-input {
  width: 100%;
  background: #1e1e2e;
  color: #cdd6f4;
  border: 1px solid #45475a;
  border-radius: 8px;
  padding: 0.75rem 1rem;
  font-size: 0.95rem;
  font-family: inherit;
  resize: vertical;
  line-height: 1.6;
  box-sizing: border-box;
  transition: border-color 0.15s;
}

.prompt-input:focus {
  outline: none;
  border-color: #89b4fa;
}

.prompt-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.hint {
  font-size: 0.75rem;
  color: #6c7086;
}

.submit-btn {
  background: #89b4fa;
  color: #1e1e2e;
  border: none;
  border-radius: 6px;
  padding: 0.45rem 1.25rem;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.15s;
  display: flex;
  align-items: center;
  gap: 0.4rem;
  min-width: 80px;
  justify-content: center;
}

.submit-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.spinner {
  width: 14px;
  height: 14px;
  border: 2px solid #1e1e2e;
  border-top-color: transparent;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
