using LlmPlayground.API.Models;
using LlmPlayground.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace LlmPlayground.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ModelsController : ControllerBase
{
    private readonly ILlmProvider _provider;

    public ModelsController(ILlmProvider provider) => _provider = provider;

    /// <summary>Returns the list of models available on the configured LLM provider.</summary>
    [HttpGet]
    public async Task<IActionResult> GetModels(CancellationToken cancellationToken)
    {
        var models = await _provider.GetModelsAsync(cancellationToken);
        return Ok(models);
    }
}
