import axios from 'axios'

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5012',
  headers: { 'Content-Type': 'application/json' }
})

export async function fetchModels() {
  const { data } = await client.get('/api/models')
  return data // array of { name, contextLength, sizeBytes }
}

export async function sendChat(payload) {
  const { data } = await client.post('/api/chat', payload)
  return data
}
