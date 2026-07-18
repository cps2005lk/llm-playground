using LlmPlayground.API.Controllers;
using LlmPlayground.API.Models;
using LlmPlayground.API.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LlmPlayground.Tests.Controllers;

[TestFixture]
public sealed class ModelsControllerTests
{
    private ILlmProvider _provider;
    private ModelsController _controller;

    [SetUp]
    public void SetUp()
    {
        _provider = Substitute.For<ILlmProvider>();
        _controller = new ModelsController(_provider);
    }

    [Test]
    public async Task GetModels_Returns200WithModelList()
    {
        var models = new List<ModelInfo>
        {
            new() { Name = "llama3.2:latest", ContextLength = 131072, SizeBytes = 2019393189 },
            new() { Name = "mistral:latest",  ContextLength = 32768,  SizeBytes = 4109854720 }
        };
        _provider.GetModelsAsync(Arg.Any<CancellationToken>()).Returns(models);

        var result = await _controller.GetModels(CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.StatusCode, Is.EqualTo(200));
        Assert.That(ok.Value, Is.SameAs(models));
    }

    [Test]
    public async Task GetModels_WhenNoModelsAvailable_ReturnsEmptyList()
    {
        _provider.GetModelsAsync(Arg.Any<CancellationToken>()).Returns([]);

        var result = await _controller.GetModels(CancellationToken.None);

        var ok = result as OkObjectResult;
        var list = ok!.Value as IReadOnlyList<ModelInfo>;
        Assert.That(list, Is.Empty);
    }

    [Test]
    public async Task GetModels_PassesCancellationTokenToProvider()
    {
        using var cts = new CancellationTokenSource();
        _provider.GetModelsAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _controller.GetModels(cts.Token);

        await _provider.Received(1).GetModelsAsync(cts.Token);
    }
}
