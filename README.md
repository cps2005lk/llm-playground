# LLM Playground

A developer tool for experimenting with LLMs. Enter a prompt, tune sampling parameters, and see the response with per-token probability highlighting. Hover any word to see the alternative tokens the model considered. A stats panel shows token usage, request timing, context window consumption, and the raw JSON request sent to the backend.

---

## Features

- **Prompt editor** with Ctrl+Enter shortcut
- **Parameter controls** — Temperature, Max Tokens, Top-P, Top-K, Model selector
- **Probability highlighting** — token backgrounds range from green (high confidence) to red (low confidence)
- **Token alternatives tooltip** — hover any token to see the top alternatives and their probabilities
- **Stats panel** — input/output/total token counts, tokens per second, request duration, context window progress bar, raw request viewer
- **Provider abstraction** — starts with Ollama; adding a new provider requires implementing one interface

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | `dotnet --version` to check |
| Node.js | 18+ | `node --version` to check |
| Ollama | Latest | [ollama.com](https://ollama.com) — must be running locally |

Pull at least one model before starting:

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
│       │   ├── ChatController.cs    # POST /api/chat
│       │   └── ModelsController.cs  # GET /api/models
│       ├── Models/
│       │   ├── ChatRequest.cs
│       │   ├── ChatResponse.cs      # ChatResponse, UsageStats, TokenData, TokenAlternative
│       │   └── ModelInfo.cs         # name, contextLength, sizeBytes
│       ├── Services/
│       │   ├── ILlmProvider.cs      # Provider interface
│       │   └── OllamaProvider.cs    # Ollama implementation
│       ├── appsettings.json         # Ollama base URL lives here
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
        │   ├── ParametersPanel.vue  # Sliders + model dropdown
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

## Configuration

### Backend — `src/LlmPlayground.API/appsettings.json`

```json
{
  "LlmProvider": {
    "Type": "Ollama",
    "BaseUrl": "http://localhost:11434",
    "ApiKey": ""
  }
}
```

Change `BaseUrl` if Ollama is running on a different host or port.

### Backend port — `src/LlmPlayground.API/Properties/launchSettings.json`

The backend is configured to run on port `5012`. Change `applicationUrl` here if you need a different port, and update `frontend/.env` to match.

### Frontend — `frontend/.env`

```
VITE_API_URL=http://localhost:5012
```

Change this if the backend runs on a different port. Vite must be restarted after changing this file.

---

## API Reference

### `GET /api/models`

Returns the list of models available on the configured Ollama instance, including context window size and model size on disk.

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
builder.Services.AddHttpClient<ILlmProvider, MyProvider>(...);
```

3. Add any new config keys under `LlmProvider` in `appsettings.json`.

No other files need to change.

---

## Tech Stack

| Concern | Technology |
|---|---|
| Backend runtime | .NET 10, ASP.NET Core Web API |
| Logging | Serilog (structured, console sink) |
| HTTP client | `IHttpClientFactory` typed client |
| Unit tests | NUnit + NSubstitute |
| Frontend framework | Vue 3 (Composition API) |
| Build tool | Vite |
| State management | Pinia |
| HTTP client (frontend) | Axios |
| LLM provider (default) | Ollama (OpenAI-compatible endpoint) |
