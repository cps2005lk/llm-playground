namespace LlmPlayground.API.Models;

public sealed record ChatRequest
{
    public string Prompt { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 512;
    public double TopP { get; init; } = 0.9;
    public int TopK { get; init; } = 40;
    public int TopLogprobs { get; init; } = 5;
}
