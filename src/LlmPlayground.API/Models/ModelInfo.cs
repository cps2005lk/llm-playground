 namespace LlmPlayground.API.Models;

public sealed record ModelInfo
{
    public string Name { get; init; } = string.Empty;
    public long ContextLength { get; init; }
    public long SizeBytes { get; init; }
}
