namespace LlmPlayground.API.Services;

public sealed class LlmProviderSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ApiKey { get; set; } = "";

    public bool IsLocal =>
        BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        BaseUrl.Contains("127.0.0.1") ||
        BaseUrl.Contains("::1");
}
