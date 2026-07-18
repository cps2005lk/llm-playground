# CLAUDE.md — LLM Playground

Read this before writing any code or making any architectural decisions.

---

## Project Overview

A developer tool for experimenting with LLMs. Users enter a prompt, tune sampling parameters, and receive a response where each token is colour-highlighted by probability. Hovering a token shows the alternative tokens the model considered. A stats panel below the response shows token usage, request timing, context window consumption, and the raw JSON request.

- **Full documentation:** `README.md`

---

## Tech Stack

| Concern | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API (controller-based) |
| Logging | Serilog, structured, console sink |
| HTTP client | `IHttpClientFactory` typed client |
| Frontend | Vue 3 (Composition API), Vite |
| State management | Pinia |
| HTTP client (frontend) | Axios (single instance in `services/api.js`) |
| LLM provider (current) | Ollama via its OpenAI-compatible endpoint (`/v1/chat/completions`) |
| Unit tests | NUnit + NSubstitute |

---

## Architecture

### Backend

Single project — no separate Application layer. The project is a developer tool, not an enterprise system.

```
src/LlmPlayground.API/
├── Controllers/
│   ├── ChatController.cs      # POST /api/chat
│   └── ModelsController.cs    # GET /api/models
├── Models/
│   ├── ChatRequest.cs
│   ├── ChatResponse.cs        # ChatResponse, UsageStats, TokenData, TokenAlternative
│   └── ModelInfo.cs           # name, contextLength, sizeBytes
└── Services/
    ├── ILlmProvider.cs        # GenerateAsync + GetModelsAsync
    └── OllamaProvider.cs      # Ollama implementation
```

**Provider pattern:** `ILlmProvider` has two methods — `GenerateAsync` and `GetModelsAsync`. To add a new provider, implement the interface and swap the registration in `Program.cs`. Nothing else changes.

**Logprobs:** The backend calls Ollama with `logprobs: true` and `top_logprobs: N`. Log-probabilities are converted to 0–1 probabilities (`exp(logprob)`) in the backend. The frontend only receives plain probability values. If a model does not return logprobs, `logprobsAvailable: false` is returned and the frontend falls back to plain text.

**Usage stats:** `OllamaProvider` measures request duration with `Stopwatch` and reads `usage.prompt_tokens` / `usage.completion_tokens` from the OpenAI-compatible response. These are returned in the `UsageStats` record on every `ChatResponse`.

**Model context length:** Ollama returns `context_length` inside the `details` object of `/api/tags` — **not** at the top level. The correct path is `m["details"]?["context_length"]`.

### Frontend

```
frontend/src/
├── App.vue                        # Layout, loads models on mount
├── main.js                        # createApp + Pinia
├── components/
│   ├── ParametersPanel.vue        # Parameter sliders + model dropdown
│   ├── PromptPanel.vue            # Textarea + submit (Ctrl+Enter)
│   ├── ResponseDisplay.vue        # Token spans with probability backgrounds
│   ├── TokenTooltip.vue           # Hover card showing alternatives
│   └── StatsPanel.vue             # Token usage, context bar, raw request viewer
├── services/api.js                # Axios wrapper — all API calls go here
└── stores/playground.js           # Pinia store — single source of truth
```

**Pinia store key computed values:**
- `selectedModel` — the full `ModelInfo` object for the currently selected model
- `contextUsedTokens` — `response.usage.totalTokens`
- `contextMaxTokens` — `selectedModel.contextLength`; context bar only renders when `> 0`
- `lastRequest` — the raw payload object sent on the last submit, shown in the raw request viewer

### Tests

```
tests/LlmPlayground.Tests/
├── Controllers/
│   ├── ChatControllerTests.cs     # 7 tests — validation, routing, error propagation
│   └── ModelsControllerTests.cs   # 3 tests — response shape, cancellation
└── Services/
    └── OllamaProviderTests.cs     # 21 tests — uses MockHttpHandler, no real Ollama needed
```

`MockHttpHandler` intercepts outbound HTTP calls by subclassing `HttpMessageHandler`. Use it for all provider tests — never spin up a real Ollama instance in unit tests.

---

## Coding Conventions

- Use Vue 3 Composition API (`<script setup>`) — never Options API
- All API calls go through `services/api.js` — never use `fetch` or create a second Axios instance
- Component state shared across components lives in the Pinia store; local UI state stays in the component
- No inline styles — use scoped `<style>` blocks with plain CSS
- Backend: use C# records for all request/response models
- Backend: `async/await` all the way through — no `.Result` or `.Wait()`
- Backend: thread `CancellationToken` from controller to provider

---

## Configuration

| File | Purpose |
|---|---|
| `src/LlmPlayground.API/appsettings.json` | Ollama base URL and API key |
| `src/LlmPlayground.API/Properties/launchSettings.json` | Backend port (currently `5012`) |
| `frontend/.env` | `VITE_API_URL` pointing at the backend (currently `http://localhost:5012`) |

---

## Hard Rules

1. **Never put business logic in a controller.** Controllers call the provider service and return results only.
2. **Never add a second LLM provider by duplicating `OllamaProvider`.** Implement `ILlmProvider` and register it — the interface exists for this reason.
3. **Never call `fetch` directly in the frontend.** All HTTP calls go through `services/api.js`.
4. **Never store secrets in `.env` files committed to the repo.** Use `appsettings.Development.json` (gitignored) for local secrets.
5. **Never convert log-probabilities to probabilities in the frontend.** That conversion happens in the backend; the frontend receives 0–1 values only.
6. **Never read `context_length` from the top-level model object.** It is nested under `details` in Ollama's `/api/tags` response.

---

## Running the Project

```bash
# Backend (from repo root)
cd src/LlmPlayground.API && dotnet run
# Starts on http://localhost:5012

# Frontend (from repo root)
cd frontend && npm run dev
# Starts on http://localhost:5173 (or 5174 if 5173 is in use)

# Tests (from repo root)
dotnet test
```

Ollama must be running on `http://localhost:11434` before starting the backend.
