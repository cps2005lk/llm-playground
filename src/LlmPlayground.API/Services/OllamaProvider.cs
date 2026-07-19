using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LlmPlayground.API.Models;

namespace LlmPlayground.API.Services;

public sealed class OllamaProvider : ILlmProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LlmProviderSettings _settings;
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(IHttpClientFactory httpClientFactory, LlmProviderSettings settings, ILogger<OllamaProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings;
        _logger = logger;
    }

    public Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default) =>
        _settings.IsLocal
            ? GenerateLocalAsync(request, cancellationToken)
            : GenerateCloudAsync(request, cancellationToken);

    // ── Local: OpenAI-compatible /v1/chat/completions with logprobs ──────────

    private async Task<ChatResponse> GenerateLocalAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = request.Model,
            messages = new[] { new { role = "user", content = request.Prompt } },
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            top_p = request.TopP,
            options = new { top_k = request.TopK },
            logprobs = true,
            top_logprobs = request.TopLogprobs,
            stream = false
        };

        var json = JsonSerializer.Serialize(body);
        var httpRequest = BuildRequest(HttpMethod.Post, "/v1/chat/completions", new StringContent(json, Encoding.UTF8, "application/json"));

        var client = _httpClientFactory.CreateClient("ollama");
        var sw = Stopwatch.StartNew();
        var response = await client.SendAsync(httpRequest, cancellationToken);
        sw.Stop();
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonNode.Parse(responseJson)!;

        var choice = doc["choices"]?[0];
        var text = choice?["message"]?["content"]?.GetValue<string>() ?? string.Empty;

        var usageNode = doc["usage"];
        var inputTokens = usageNode?["prompt_tokens"]?.GetValue<int>() ?? 0;
        var outputTokens = usageNode?["completion_tokens"]?.GetValue<int>() ?? 0;
        var durationMs = sw.Elapsed.TotalMilliseconds;

        var usage = new UsageStats
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = usageNode?["total_tokens"]?.GetValue<int>() ?? 0,
            DurationMs = Math.Round(durationMs, 0),
            TokensPerSecond = durationMs > 0 ? Math.Round(outputTokens / (durationMs / 1000.0), 1) : 0
        };

        var logprobsNode = choice?["logprobs"]?["content"];
        if (logprobsNode is null)
        {
            _logger.LogWarning("Model {Model} did not return logprobs", request.Model);
            return new ChatResponse { Text = text, LogprobsAvailable = false, Usage = usage };
        }

        var tokens = new List<TokenData>();
        foreach (var tokenNode in logprobsNode.AsArray())
        {
            if (tokenNode is null) continue;

            var tokenText = tokenNode["token"]?.GetValue<string>() ?? string.Empty;
            var logprob = tokenNode["logprob"]?.GetValue<double>() ?? double.NegativeInfinity;
            var probability = Math.Exp(logprob);

            var alternatives = new List<TokenAlternative>();
            var topLogprobsNode = tokenNode["top_logprobs"]?.AsArray();
            if (topLogprobsNode is not null)
            {
                foreach (var alt in topLogprobsNode)
                {
                    if (alt is null) continue;
                    var altText = alt["token"]?.GetValue<string>() ?? string.Empty;
                    var altLogprob = alt["logprob"]?.GetValue<double>() ?? double.NegativeInfinity;
                    alternatives.Add(new TokenAlternative
                    {
                        Text = altText,
                        Probability = Math.Round(Math.Exp(altLogprob), 4)
                    });
                }
            }

            tokens.Add(new TokenData
            {
                Text = tokenText,
                Probability = Math.Round(probability, 4),
                Alternatives = alternatives
            });
        }

        return new ChatResponse { Text = text, LogprobsAvailable = true, Tokens = tokens, Usage = usage };
    }

    // ── Cloud: native Ollama /api/generate ───────────────────────────────────
    // Response shape: { response, prompt_eval_count, eval_count, eval_duration (ns) }
    // Logprobs are not available via this endpoint.

    private async Task<ChatResponse> GenerateCloudAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = request.Model,
            prompt = request.Prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(body);
        var httpRequest = BuildRequest(HttpMethod.Post, "/api/generate", new StringContent(json, Encoding.UTF8, "application/json"));

        var client = _httpClientFactory.CreateClient("ollama");
        var sw = Stopwatch.StartNew();
        var response = await client.SendAsync(httpRequest, cancellationToken);
        sw.Stop();
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var doc = JsonNode.Parse(responseJson)!;

        var text = doc["response"]?.GetValue<string>() ?? string.Empty;
        var inputTokens = doc["prompt_eval_count"]?.GetValue<int>() ?? 0;
        var outputTokens = doc["eval_count"]?.GetValue<int>() ?? 0;
        var durationMs = sw.Elapsed.TotalMilliseconds;

        var usage = new UsageStats
        {
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
            DurationMs = Math.Round(durationMs, 0),
            TokensPerSecond = durationMs > 0 ? Math.Round(outputTokens / (durationMs / 1000.0), 1) : 0
        };

        return new ChatResponse { Text = text, LogprobsAvailable = false, Usage = usage };
    }

    // ── Models ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ollama");
            var request = BuildRequest(HttpMethod.Get, "/api/tags");
            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonNode.Parse(json)!;

            return doc["models"]?.AsArray()
                .Where(m => m is not null)
                .Select(m => new ModelInfo
                {
                    Name = m!["name"]?.GetValue<string>() ?? string.Empty,
                    ContextLength = m["details"]?["context_length"]?.GetValue<long>() ?? 0,
                    SizeBytes = m["size"]?.GetValue<long>() ?? 0
                })
                .Where(m => !string.IsNullOrEmpty(m.Name))
                .ToList()
                ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch models from Ollama ({BaseUrl})", _settings.BaseUrl);
            return [];
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, HttpContent? content = null)
    {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');
        var request = new HttpRequestMessage(method, $"{baseUrl}{path}");

        if (!string.IsNullOrEmpty(_settings.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        if (content is not null)
            request.Content = content;

        return request;
    }
}
