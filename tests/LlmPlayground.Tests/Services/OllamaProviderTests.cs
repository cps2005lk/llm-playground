using System.Net;
using System.Text;
using System.Text.Json;
using LlmPlayground.API.Models;
using LlmPlayground.API.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LlmPlayground.Tests.Services;

/// <summary>
/// Intercepts outbound HTTP calls so OllamaProvider can be tested without a running Ollama instance.
/// </summary>
internal sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;
    public HttpRequestMessage? LastRequest { get; private set; }

    public MockHttpHandler(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _response = new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_response);
    }
}

[TestFixture]
public sealed class OllamaProviderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static OllamaProvider BuildProvider(string responseJson, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new MockHttpHandler(responseJson, status);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        return new OllamaProvider(http, NullLogger<OllamaProvider>.Instance);
    }

    private static string ChatJson(
        string text,
        int inputTokens = 10,
        int outputTokens = 5,
        bool includeLogprobs = true,
        double[]? logprobValues = null) =>
        JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new { role = "assistant", content = text },
                    logprobs = includeLogprobs ? new
                    {
                        content = (logprobValues ?? [-0.1, -0.5, -2.3]).Select(lp => new
                        {
                            token = "word",
                            logprob = lp,
                            top_logprobs = new[]
                            {
                                new { token = "word",  logprob = lp },
                                new { token = "other", logprob = lp - 1.5 }
                            }
                        }).ToArray()
                    } : (object?)null
                }
            },
            usage = new
            {
                prompt_tokens    = inputTokens,
                completion_tokens = outputTokens,
                total_tokens     = inputTokens + outputTokens
            }
        });

    private static string ModelsJson() => """
        {
          "models": [
            {
              "name": "llama3.2:latest",
              "size": 2019393189,
              "context_length": 131072,
              "details": { "parameter_size": "3.2B", "family": "llama" }
            },
            {
              "name": "mistral:latest",
              "size": 4109854720,
              "context_length": 32768,
              "details": { "parameter_size": "7B", "family": "mistral" }
            }
          ]
        }
        """;

    private static ChatRequest DefaultRequest(string prompt = "Hello") =>
        new() { Prompt = prompt, Model = "llama3.2", Temperature = 0.7, MaxTokens = 512, TopP = 0.9, TopK = 40 };

    // ── GenerateAsync — text ─────────────────────────────────────────────────

    [Test]
    public async Task GenerateAsync_ReturnsCorrectResponseText()
    {
        var provider = BuildProvider(ChatJson("The sky is blue."));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Text, Is.EqualTo("The sky is blue."));
    }

    [Test]
    public async Task GenerateAsync_SetsLogprobsAvailableTrue_WhenLogprobsPresent()
    {
        var provider = BuildProvider(ChatJson("Hello", includeLogprobs: true));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.LogprobsAvailable, Is.True);
    }

    [Test]
    public async Task GenerateAsync_SetsLogprobsAvailableFalse_WhenLogprobsMissing()
    {
        var provider = BuildProvider(ChatJson("Hello", includeLogprobs: false));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.LogprobsAvailable, Is.False);
        Assert.That(result.Tokens, Is.Empty);
    }

    // ── GenerateAsync — token probabilities ──────────────────────────────────

    [Test]
    public async Task GenerateAsync_ConvertLogprobToProbabilityCorrectly()
    {
        // logprob = 0.0 → probability = exp(0) = 1.0
        var provider = BuildProvider(ChatJson("Hi", logprobValues: [0.0]));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Tokens[0].Probability, Is.EqualTo(1.0).Within(0.0001));
    }

    [Test]
    public async Task GenerateAsync_HighNegativeLogprob_ProducesLowProbability()
    {
        // logprob = -10 → probability ≈ 0.0000454
        var provider = BuildProvider(ChatJson("Hi", logprobValues: [-10.0]));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Tokens[0].Probability, Is.LessThan(0.001));
    }

    [Test]
    public async Task GenerateAsync_ReturnsCorrectNumberOfTokens()
    {
        var logprobs = new double[] { -0.1, -0.5, -2.3, -0.9 };
        var provider = BuildProvider(ChatJson("text", logprobValues: logprobs));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Tokens, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task GenerateAsync_EachTokenHasAlternatives()
    {
        var provider = BuildProvider(ChatJson("Hi", logprobValues: [-0.1]));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Tokens[0].Alternatives, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GenerateAsync_AlternativeProbabilitiesAreLowerThanTopToken()
    {
        // top token logprob = -0.1 (prob ≈ 0.9048), second = -1.6 (prob ≈ 0.2019)
        var provider = BuildProvider(ChatJson("Hi", logprobValues: [-0.1]));

        var result = await provider.GenerateAsync(DefaultRequest());

        var top = result.Tokens[0];
        Assert.That(top.Alternatives[1].Probability, Is.LessThan(top.Probability));
    }

    // ── GenerateAsync — usage stats ──────────────────────────────────────────

    [Test]
    public async Task GenerateAsync_MapsInputTokensCorrectly()
    {
        var provider = BuildProvider(ChatJson("Hi", inputTokens: 42, outputTokens: 10));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Usage.InputTokens, Is.EqualTo(42));
    }

    [Test]
    public async Task GenerateAsync_MapsOutputTokensCorrectly()
    {
        var provider = BuildProvider(ChatJson("Hi", inputTokens: 5, outputTokens: 99));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Usage.OutputTokens, Is.EqualTo(99));
    }

    [Test]
    public async Task GenerateAsync_TotalTokensEqualsSumOfInputAndOutput()
    {
        var provider = BuildProvider(ChatJson("Hi", inputTokens: 15, outputTokens: 35));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Usage.TotalTokens, Is.EqualTo(50));
    }

    [Test]
    public async Task GenerateAsync_DurationMsIsPositive()
    {
        var provider = BuildProvider(ChatJson("Hi"));

        var result = await provider.GenerateAsync(DefaultRequest());

        // Mock handler responds instantly so duration may round to 0ms — assert non-negative.
        // Against a real Ollama instance this will always be > 0.
        Assert.That(result.Usage.DurationMs, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task GenerateAsync_TokensPerSecondIsPositive_WhenOutputTokensExist()
    {
        var provider = BuildProvider(ChatJson("Hi", outputTokens: 20));

        var result = await provider.GenerateAsync(DefaultRequest());

        Assert.That(result.Usage.TokensPerSecond, Is.GreaterThan(0));
    }

    // ── GenerateAsync — HTTP ─────────────────────────────────────────────────

    [Test]
    public void GenerateAsync_WhenHttpReturns500_ThrowsHttpRequestException()
    {
        var provider = BuildProvider("{}", HttpStatusCode.InternalServerError);

        Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.GenerateAsync(DefaultRequest()));
    }

    [Test]
    public async Task GenerateAsync_SendsRequestToCorrectEndpoint()
    {
        var handler = new MockHttpHandler(ChatJson("ok"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var provider = new OllamaProvider(http, NullLogger<OllamaProvider>.Instance);

        await provider.GenerateAsync(DefaultRequest());

        Assert.That(handler.LastRequest!.RequestUri!.AbsolutePath, Is.EqualTo("/v1/chat/completions"));
    }

    [Test]
    public async Task GenerateAsync_SendsPostRequest()
    {
        var handler = new MockHttpHandler(ChatJson("ok"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434") };
        var provider = new OllamaProvider(http, NullLogger<OllamaProvider>.Instance);

        await provider.GenerateAsync(DefaultRequest());

        Assert.That(handler.LastRequest!.Method, Is.EqualTo(HttpMethod.Post));
    }

    // ── GetModelsAsync ───────────────────────────────────────────────────────

    [Test]
    public async Task GetModelsAsync_ReturnsCorrectModelCount()
    {
        var provider = BuildProvider(ModelsJson());

        var models = await provider.GetModelsAsync();

        Assert.That(models, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetModelsAsync_ReturnsCorrectModelNames()
    {
        var provider = BuildProvider(ModelsJson());

        var models = await provider.GetModelsAsync();

        Assert.That(models.Select(m => m.Name), Is.EquivalentTo(new[]
        {
            "llama3.2:latest",
            "mistral:latest"
        }));
    }

    [Test]
    public async Task GetModelsAsync_ReturnsSizeBytes()
    {
        var provider = BuildProvider(ModelsJson());

        var models = await provider.GetModelsAsync();

        Assert.That(models[0].SizeBytes, Is.EqualTo(2019393189L));
    }

    [Test]
    public async Task GetModelsAsync_WhenHttpFails_ReturnsEmptyList()
    {
        var provider = BuildProvider("{}", HttpStatusCode.ServiceUnavailable);

        var models = await provider.GetModelsAsync();

        Assert.That(models, Is.Empty);
    }

    [Test]
    public async Task GetModelsAsync_WhenResponseIsEmptyModelsArray_ReturnsEmptyList()
    {
        var provider = BuildProvider("""{ "models": [] }""");

        var models = await provider.GetModelsAsync();

        Assert.That(models, Is.Empty);
    }
}
