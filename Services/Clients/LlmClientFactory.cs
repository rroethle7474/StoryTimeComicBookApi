using Microsoft.Extensions.DependencyInjection;
using StoryTimeComicBookApi.Services.Clients.Interfaces;

namespace StoryTimeComicBookApi.Services.Clients;

public static class LlmClientFactory
{
    public static void AddLlmClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILlmClient>(provider =>
        {
            var llmType = configuration["AI:LlmType"] ?? "Gemini";
            var apiKey = configuration[$"AI:{llmType}:ApiKey"] ?? 
                throw new InvalidOperationException($"Missing API key for {llmType}");

            return llmType.ToLowerInvariant() switch
            {
                "gemini" => new GeminiApiClient(apiKey),
                "openai" => throw new NotImplementedException("OpenAI implementation pending"),
                _ => throw new InvalidOperationException($"Unsupported LLM type: {llmType}")
            };
        });
    }
} 