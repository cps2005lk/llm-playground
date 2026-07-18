import { defineStore } from 'pinia'
import { ref, reactive, computed } from 'vue'
import { fetchModels, sendChat } from '../services/api'

export const usePlaygroundStore = defineStore('playground', () => {
  const models = ref([])          // [{ name, contextLength, sizeBytes }]
  const loading = ref(false)
  const error = ref(null)
  const response = ref(null)
  const lastRequest = ref(null)   // raw JSON sent to the backend

  const params = reactive({
    model: '',
    temperature: 0.7,
    maxTokens: 512,
    topP: 0.9,
    topK: 40
  })

  const selectedModel = computed(() =>
    models.value.find(m => m.name === params.model) || null
  )

  const contextUsedTokens = computed(() =>
    response.value?.usage?.totalTokens ?? 0
  )

  const contextMaxTokens = computed(() =>
    selectedModel.value?.contextLength ?? 0
  )

  async function loadModels() {
    try {
      models.value = await fetchModels()
      if (models.value.length && !params.model) {
        params.model = models.value[0].name
      }
    } catch (e) {
      error.value = 'Could not load models. Is Ollama running?'
    }
  }

  async function submit(prompt) {
    error.value = null
    response.value = null
    loading.value = true

    const payload = {
      prompt,
      model: params.model,
      temperature: params.temperature,
      maxTokens: params.maxTokens,
      topP: params.topP,
      topK: params.topK
    }
    lastRequest.value = payload

    try {
      response.value = await sendChat(payload)
    } catch (e) {
      error.value = e.response?.data?.error || e.message || 'Request failed.'
    } finally {
      loading.value = false
    }
  }

  return {
    models, loading, error, response, lastRequest,
    params, selectedModel, contextUsedTokens, contextMaxTokens,
    loadModels, submit
  }
})
