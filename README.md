# LLM Playground

A developer tool for experimenting with LLMs. Enter a prompt, tune sampling parameters, and see the response with per-token probability highlighting. Hover any word to see the alternative tokens the model considered. A stats panel shows token usage, request timing, context window consumption, and the raw JSON request sent to the backend.

---

## Features

- **Prompt editor** with Ctrl+Enter shortcut
- **Parameter controls** — Temperature, Max Tokens, Top-P, Top-K, Model selector
- **Probability highlighting** — token backgrounds range from green (high confidence) to red (low confidence)
- **Token alternatives tooltip** — hover any token to see the top alternatives and their probabilities
- **Stats panel** — input/output/total token counts, tokens per second, request duration, context window progress bar, raw request viewer
- **Runtime provider switching** — switch between local Ollama and Ollama cloud without restarting the app; the model list refreshes automatically
- **Provider abstraction** — starts with Ollama; adding a new provider requires implementing one interface

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` to check |
| Node.js | 18+ | `node --version` to check |
| Ollama | Latest | [ollama.com](https://ollama.com) — local or cloud |

For local mode, pull at least one model before starting:

```bash
ollama pull llama3.2
```

---

## Project Structure

```
LLM Play Ground/
├── LlmPlayground.slnx               # .NET solution
├── src/
│   └── LlmPlayground.API/           # ASP.NET Core Web API (.NET 10)
│       ├── Controllers/
│       │   ├── ChatController.cs        # POST /api/chat
│       │   ├── ModelsController.cs      # GET /api/models
│       │   └── ProviderConfigController.cs  # GET/PUT /api/provider-config
│       ├── Models/
│       │   ├── ChatRequest.cs
│       │   ├── ChatResponse.cs          # ChatResponse, UsageStats, TokenData, TokenAlternative
│       │   ├── ModelInfo.cs             # name, contextLength, sizeBytes
│       │   └── ProviderConfigRequest.cs
│       ├── Services/
│       │   ├── ILlmProvider.cs          # Provider interface
│       │   ├── LlmProviderSettings.cs   # Mutable runtime settings (BaseUrl, ApiKey)
│       │   └── OllamaProvider.cs        # Ollama implementation
│       ├── appsettings.json             # Initial provider URL and API key
│       └── Program.cs
├── tests/
│   └── LlmPlayground.Tests/         # NUnit + NSubstitute unit tests
│       ├── Controllers/
│       │   ├── ChatControllerTests.cs
│       │   └── ModelsControllerTests.cs
│       └── Services/
│           └── OllamaProviderTests.cs
└── frontend/                        # Vue 3 + Vite + Pinia
    └── src/
        ├── App.vue
        ├── main.js
        ├── components/
        │   ├── ParametersPanel.vue  # Sliders, model dropdown, connection settings
        │   ├── PromptPanel.vue      # Textarea + submit button
        │   ├── ResponseDisplay.vue  # Token-highlighted output
        │   ├── TokenTooltip.vue     # Hover alternatives card
        │   └── StatsPanel.vue       # Token usage, context bar, raw request viewer
        ├── services/
        │   └── api.js               # Axios wrapper
        └── stores/
            └── playground.js        # Pinia store
```

---

## Running Locally

### 1. Start Ollama

```bash
ollama serve
```

### 2. Start the backend

```bash
cd "src/LlmPlayground.API"
dotnet run
```

The API starts on `http://localhost:5012`.

### 3. Start the frontend

```bash
cd frontend
npm install   # first time only
npm run dev
```

The UI opens at `http://localhost:5173` (or `5174` if `5173` is already in use).

### 4. Run the tests

```bash
dotnet test
```

---

## Switching Between Local and Cloud

The **Connection** section at the bottom of the left panel lets you switch providers while the app is running — no restart needed.

**Local Ollama** (default)

Click **Local** — the Base URL is set to `http://localhost:11434` and no API key is required. Click **Apply** to connect and refresh the model list.

**Ollama Cloud**

1. Click **Cloud** — the Base URL switches to `https://ollama.com`.
2. Paste your API key into the **API Key** field (obtain one at [ollama.com](https://ollama.com)).
3. Click **Apply** — the backend updates immediately and the model list refreshes.

The connection change takes effect instantly via `PUT /api/provider-config`. You can switch back to local at any time by clicking **Local** and **Apply**.

---

## Configuration

### Initial provider settings — `src/LlmPlayground.API/appsettings.json`

```json
{
  "LlmProvider": {
    "BaseUrl": "http://localhost:11434",
    "ApiKey": ""
  }
}
```

These values are the startup defaults. After the app is running, the active settings can be changed through the UI or the `PUT /api/provider-config` endpoint without a restart.

For local secrets such as an API key, use `appsettings.Development.json` (gitignored) rather than committing to `appsettings.json`.

### Backend port — `src/LlmPlayground.API/Properties/launchSettings.json`

The backend runs on port `5012`. Change `applicationUrl` here if needed, then update `frontend/.env` to match.

### Frontend — `frontend/.env`

```
VITE_API_URL=http://localhost:5012
```

Change this if the backend runs on a different port. Vite must be restarted after editing this file.

---

## API Reference

### `GET /api/models`

Returns the list of models available on the currently configured provider.

**Response**
```json
[
  {
    "name": "llama3.2:latest",
    "contextLength": 131072,
    "sizeBytes": 2019393189
  }
]
```

For cloud providers, `contextLength` and `sizeBytes` are `0` (the cloud API does not expose those fields).

---

### `POST /api/chat`

Sends a prompt and returns the full response with per-token probability data and usage statistics.

**Request body**
```json
{
  "prompt": "Why is the sky blue?",
  "model": "llama3.2:latest",
  "temperature": 0.7,
  "maxTokens": 512,
  "topP": 0.9,
  "topK": 40,
  "topLogprobs": 5
}
```

**Response**
```json
{
  "text": "The sky appears blue because…",
  "logprobsAvailable": true,
  "usage": {
    "inputTokens": 12,
    "outputTokens": 54,
    "totalTokens": 66,
    "durationMs": 3420,
    "tokensPerSecond": 15.8
  },
  "tokens": [
    {
      "text": "The",
      "probability": 0.9312,
      "alternatives": [
        { "text": "A",   "probability": 0.0421 },
        { "text": "Sky", "probability": 0.0098 }
      ]
    }
  ]
}
```

`logprobsAvailable` is `false` when the model does not return logprobs. In that case `tokens` is empty and the frontend shows plain text without highlighting.

---

### `GET /api/provider-config`

Returns the currently active provider settings. The API key is never echoed back.

**Response**
```json
{
  "baseUrl": "http://localhost:11434",
  "hasApiKey": false
}
```

---

### `PUT /api/provider-config`

Updates the provider settings at runtime. The change takes effect immediately for all subsequent requests.

**Request body**
```json
{
  "baseUrl": "https://ollama.com",
  "apiKey": "your-api-key"
}
```

Returns `204 No Content` on success.

---

## Adding a New LLM Provider

1. Create `src/LlmPlayground.API/Services/<Name>Provider.cs` implementing `ILlmProvider`:

```csharp
public sealed class MyProvider : ILlmProvider
{
    public Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken ct) { ... }
    public Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken ct) { ... }
}
```

2. Register it in `Program.cs` in place of `OllamaProvider`:

```csharp
builder.Services.AddScoped<ILlmProvider, MyProvider>();
```

3. Add any new config keys under `LlmProvider` in `appsettings.json`.

No other files need to change.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Backend runtime | .NET 10, ASP.NET Core Web API |
| Logging | Serilog (structured, console sink) |
| HTTP client | `IHttpClientFactory` (named client, per-request headers) |
| Unit tests | NUnit + NSubstitute |
| Frontend framework | Vue 3 (Composition API) |
| Build tool | Vite |
| State management | Pinia |
| HTTP client (frontend) | Axios |
| LLM provider (default) | Ollama — local or cloud |
