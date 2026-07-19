namespace LlmPlayground.API.Models;

public sealed record ProviderConfigRequest
{
    public string BaseUrl { get; init; } = string.Empty;
    public string? ApiKey { get; init; }
}
