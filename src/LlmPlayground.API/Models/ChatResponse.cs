namespace LlmPlayground.API.Models;

public sealed record ChatResponse
{
    public string Text { get; init; } = string.Empty;
    public bool LogprobsAvailable { get; init; }
    public List<TokenData> Tokens { get; init; } = [];
    public UsageStats Usage { get; init; } = new();
}

public sealed record UsageStats
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int TotalTokens { get; init; }
    public double DurationMs { get; init; }
    public double TokensPerSecond { get; init; }
}

public sealed record TokenData
{
    public string Text { get; init; } = string.Empty;
    public double Probability { get; init; }
    public List<TokenAlternative> Alternatives { get; init; } = [];
}

public sealed record TokenAlternative
{
    public string Text { get; init; } = string.Empty;
    public double Probability { get; init; }
}
