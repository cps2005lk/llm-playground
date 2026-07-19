using LlmPlayground.API.Models;
using LlmPlayground.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace LlmPlayground.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly ILlmProvider _provider;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ILlmProvider provider, ILogger<ChatController> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    /// <summary>Sends a prompt to the LLM and returns the response with per-token probability data.</summary>
    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return BadRequest(new { error = "Prompt must not be empty." });

        if (string.IsNullOrWhiteSpace(request.Model))
            return BadRequest(new { error = "Model must be specified." });

        _logger.LogInformation("Chat request: model={Model} temp={Temp}", request.Model, request.Temperature);

        try
        {
            var response = await _provider.GenerateAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (LlmProviderException ex)
        {
            _logger.LogWarning("Provider returned {StatusCode}: {Message}", ex.StatusCode, ex.Message);
            return StatusCode(ex.StatusCode, new { error = ex.Message });
        }
    }
}
