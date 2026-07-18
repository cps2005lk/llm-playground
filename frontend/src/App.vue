<template>
  <div class="app">
    <header class="app-header">
      <span class="logo">LLM Playground</span>
      <span v-if="store.error" class="error-banner">{{ store.error }}</span>
    </header>

    <div class="app-body">
      <ParametersPanel />

      <main class="main-area">
        <PromptPanel />
        <ResponseDisplay :response="store.response" />
        <StatsPanel />
      </main>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import ParametersPanel from './components/ParametersPanel.vue'
import PromptPanel from './components/PromptPanel.vue'
import ResponseDisplay from './components/ResponseDisplay.vue'
import StatsPanel from './components/StatsPanel.vue'
import { usePlaygroundStore } from './stores/playground'

const store = usePlaygroundStore()
onMounted(() => store.loadModels())
</script>

<style>
*, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

body {
  background: #11111b;
  color: #cdd6f4;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui, sans-serif;
  height: 100vh;
  overflow: hidden;
}

#app { height: 100vh; display: flex; flex-direction: column; }
</style>

<style scoped>
.app { height: 100vh; display: flex; flex-direction: column; }

.app-header {
  background: #1e1e2e;
  border-bottom: 1px solid #313244;
  padding: 0.75rem 1.5rem;
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-shrink: 0;
}

.logo {
  font-size: 1rem;
  font-weight: 600;
  color: #89b4fa;
  letter-spacing: 0.02em;
}

.error-banner {
  font-size: 0.82rem;
  color: #f38ba8;
  background: rgba(243, 139, 168, 0.12);
  border: 1px solid rgba(243, 139, 168, 0.3);
  border-radius: 4px;
  padding: 0.2rem 0.6rem;
}

.app-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.main-area {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding: 1.25rem 1.5rem;
  overflow-y: auto;
}
</style>
