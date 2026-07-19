using LlmPlayground.API.Models;
using LlmPlayground.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace LlmPlayground.API.Controllers;

[ApiController]
[Route("api/provider-config")]
public sealed class ProviderConfigController : ControllerBase
{
    private readonly LlmProviderSettings _settings;

    public ProviderConfigController(LlmProviderSettings settings) => _settings = settings;

    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        baseUrl = _settings.BaseUrl,
        hasApiKey = !string.IsNullOrEmpty(_settings.ApiKey)
    });

    [HttpPut]
    public IActionResult Put([FromBody] ProviderConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BaseUrl))
            return BadRequest("BaseUrl is required.");

        _settings.BaseUrl = request.BaseUrl.TrimEnd('/');
        _settings.ApiKey = request.ApiKey ?? string.Empty;

        return NoContent();
    }
}
