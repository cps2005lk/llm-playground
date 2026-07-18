using LlmPlayground.API.Models;


namespace LlmPlayground.API.Services;

public interface ILlmProvider
{
    Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModelInfo>> GetModelsAsync(CancellationToken cancellationToken = default);
}
