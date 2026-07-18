# LLM Playground — Phase Status

This document tracks what has been built and what is planned for future phases.

---

## Phase 1 — Core Stats ✅ Complete

**Goal:** Make token usage, request performance, and context window consumption visible after every request. Give the user a way to see exactly what was sent to the backend.

### Features built

| # | Feature | Where |
|---|---|---|
| 1 | Input tokens, output tokens, total tokens per request | `StatsPanel.vue` → Token Usage section |
| 2 | Request duration (seconds) | `StatsPanel.vue` → Token Usage section |
| 3 | Tokens per second | `StatsPanel.vue` → Token Usage section |
| 4 | Context window progress bar (tokens used vs model maximum, green→yellow→red) | `StatsPanel.vue` → Context Window section |
| 5 | Raw request viewer (exact JSON sent to backend, show/hide toggle) | `StatsPanel.vue` → Raw Request section |

### Backend changes
- `UsageStats` record added to `ChatResponse` — `inputTokens`, `outputTokens`, `totalTokens`, `durationMs`, `tokensPerSecond`
- `ModelInfo` record introduced — `name`, `contextLength`, `sizeBytes`
- `GET /api/models` now returns `ModelInfo[]` instead of `string[]`
- `OllamaProvider` reads `context_length` from `details` object in Ollama's `/api/tags` response
- Request duration measured with `Stopwatch` in `OllamaProvider`

### Tests
- 31 NUnit unit tests across `ChatControllerTests`, `ModelsControllerTests`, `OllamaProviderTests`
- `MockHttpHandler` pattern established for testing `OllamaProvider` without a live Ollama instance

---

## Phase 2 — Request History & Comparison 📋 Planned

**Goal:** Let the user see all requests made in the session and compare how different parameters affect the same prompt side by side.

### Features planned

| # | Feature | Notes |
|---|---|---|
| 6 | Session call log | Running list of every request — model, parameters, token counts, timestamp. Expandable to see full prompt and response. |
| 7 | Request timeline | Visual timeline showing each request, its duration, and token count across the session. |
| 8 | Side-by-side comparison mode | Send the same prompt with two different parameter sets and display both responses with their token highlighting and stats next to each other. Most effective way to understand what Temperature, Top-P, and Top-K actually do. |

---

## Phase 3 — Deeper Model Insight 📋 Planned

**Goal:** Surface information that explains *why* the model responded the way it did — confidence patterns, surprising choices, and vocabulary richness.

### Features planned

| # | Feature | Notes |
|---|---|---|
| 9 | Average token probability + mini histogram | What percentage of tokens fell into each confidence band (e.g. 30% were >90% confident, 15% were <30% confident). High average = model was sure. Low average = model was guessing — more likely to hallucinate. |
| 10 | Confidence drop zones | Highlight sentences (not just tokens) where average probability fell below a threshold. Visual hotspot for potential hallucinations. |
| 11 | Most surprising token callout | Identify the single lowest-probability token in the response and call it out — "The model was least sure about this word (3.2% confidence)." |
| 12 | Response diversity score | Unique tokens ÷ total tokens. High at high temperature, low at low temperature. Makes the Temperature parameter intuitive. |
| 13 | Token entropy per response | Entropy measures how spread-out the probability was across all possible tokens. More informative than probability alone for understanding model confidence. |

---

## Phase 4 — Advanced Learning Tools 📋 Planned

**Goal:** Close the loop between parameters, model behaviour, and cost — and introduce multi-turn conversation so the context window concept becomes tangible.

### Features planned

| # | Feature | Notes |
|---|---|---|
| 14 | Parameter effect indicators | Badges next to each slider showing whether the current value is "safe", "creative", or "risky" with a one-line explanation. E.g. Temperature 1.8 → "⚠ Very high — expect unpredictable output". |
| 15 | Multi-turn conversation mode | Toggle that sends the full conversation history with each message. Context bar fills up as the conversation continues — makes context window limits tangible. |
| 16 | Model memory usage estimate | Estimated RAM the model is using (model size × quantisation factor). Makes hardware constraints visible. |
| 17 | Estimated cloud cost equivalent | "If you ran this on OpenAI GPT-4o, this request would have cost $0.003." Educational only — helps appreciate the value of running locally. |
| 18 | Token cost breakdown bar | Visual split of input vs output tokens. Output tokens cost more with cloud providers — seeing the split makes that concrete. |
| 19 | Parameter history chart | Line chart of how Temperature, Top-P etc. changed across session requests, plotted against average probability or tokens/sec. Lets the user spot correlations visually. |

---

## Feature Backlog (undated)

Ideas discussed but not yet assigned to a phase:

- **Prompt length vs response length scatter plot** — does a longer prompt produce a longer response? Plotted across the session.
- **Repetition penalty visualiser** — show which tokens would have been repeated without the penalty.
- **"View raw response" toggle** — show the full JSON received from the backend alongside the raw request viewer.
- **System prompt support** — persistent instruction prepended to every request, with a token cost indicator showing how many context tokens it consumes before you type anything.

---

## Summary

| Phase | Status | Features |
|---|---|---|
| Phase 1 — Core Stats | ✅ Complete | 5 features |
| Phase 2 — Request History & Comparison | 📋 Planned | 3 features |
| Phase 3 — Deeper Model Insight | 📋 Planned | 5 features |
| Phase 4 — Advanced Learning Tools | 📋 Planned | 6 features |
| Backlog | 💡 Ideas | 4 items |
