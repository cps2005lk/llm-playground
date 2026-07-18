using LlmPlayground.API.Controllers;
using LlmPlayground.API.Models;
using LlmPlayground.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LlmPlayground.Tests.Controllers;

[TestFixture]
public sealed class ChatControllerTests
{
    private ILlmProvider _provider;
    private ChatController _controller;

    [SetUp]
    public void SetUp()
    {
        _provider = Substitute.For<ILlmProvider>();
        _controller = new ChatController(_provider, NullLogger<ChatController>.Instance);
    }

    [Test]
    public async Task Chat_WithValidRequest_Returns200WithResponse()
    {
        var request = new ChatRequest { Prompt = "Why is the sky blue?", Model = "llama3.2" };
        var expectedResponse = new ChatResponse
        {
            Text = "The sky appears blue because of Rayleigh scattering.",
            LogprobsAvailable = true,
            Tokens = [new TokenData { Text = "The", Probability = 0.95 }],
            Usage = new UsageStats { InputTokens = 10, OutputTokens = 8, TotalTokens = 18, DurationMs = 1200, TokensPerSecond = 6.7 }
        };
        _provider.GenerateAsync(request, Arg.Any<CancellationToken>()).Returns(expectedResponse);

        var result = await _controller.Chat(request, CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.SameAs(expectedResponse));
    }

    [Test]
    public async Task Chat_WithEmptyPrompt_Returns400()
    {
        var request = new ChatRequest { Prompt = "", Model = "llama3.2" };

        var result = await _controller.Chat(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _provider.DidNotReceive().GenerateAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Chat_WithWhitespacePrompt_Returns400()
    {
        var request = new ChatRequest { Prompt = "   ", Model = "llama3.2" };

        var result = await _controller.Chat(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Chat_WithEmptyModel_Returns400()
    {
        var request = new ChatRequest { Prompt = "Hello", Model = "" };

        var result = await _controller.Chat(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        await _provider.DidNotReceive().GenerateAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Chat_WhenProviderThrows_PropagatesException()
    {
        var request = new ChatRequest { Prompt = "Hello", Model = "llama3.2" };
        _provider.GenerateAsync(request, Arg.Any<CancellationToken>())
                 .ThrowsAsync(new HttpRequestException("Ollama unreachable"));

        Assert.ThrowsAsync<HttpRequestException>(() => _controller.Chat(request, CancellationToken.None));
    }

    [Test]
    public async Task Chat_PassesRequestToProviderUnmodified()
    {
        var request = new ChatRequest
        {
            Prompt = "Test prompt",
            Model = "llama3.2",
            Temperature = 0.5,
            MaxTokens = 256,
            TopP = 0.8,
            TopK = 20
        };
        _provider.GenerateAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
                 .Returns(new ChatResponse { Text = "ok" });

        await _controller.Chat(request, CancellationToken.None);

        await _provider.Received(1).GenerateAsync(
            Arg.Is<ChatRequest>(r =>
                r.Prompt == "Test prompt" &&
                r.Model == "llama3.2" &&
                r.Temperature == 0.5 &&
                r.MaxTokens == 256 &&
                r.TopP == 0.8 &&
                r.TopK == 20),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Chat_WithLogprobsUnavailable_Returns200WithFlagFalse()
    {
        var request = new ChatRequest { Prompt = "Hello", Model = "llama3.2" };
        _provider.GenerateAsync(request, Arg.Any<CancellationToken>())
                 .Returns(new ChatResponse { Text = "Hi there!", LogprobsAvailable = false });

        var result = await _controller.Chat(request, CancellationToken.None);

        var ok = result as OkObjectResult;
        var response = ok!.Value as ChatResponse;
        Assert.That(response!.LogprobsAvailable, Is.False);
        Assert.That(response.Text, Is.EqualTo("Hi there!"));
    }
}
