// Updated LlmClientFactory.cs
using StoryTimeComicBookApi.Services.Clients.Interfaces;

namespace StoryTimeComicBookApi.Services.Clients;

public static class LlmClientFactory
{

    public static void AddLlmClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILlmClient>(provider =>
        {
            var llmType = configuration["AI:LlmType"] ?? "Gemini";
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

            return llmType.ToLowerInvariant() switch
            {
                "gemini" => new GeminiApiClient(configuration["AI:Gemini:ApiKey"] ??
                    throw new InvalidOperationException("Missing API key for Gemini")),
                "openai" => new OpenAIApiClient(httpClientFactory, configuration),
                "anthropic" => new AnthropicApiClient(httpClientFactory, configuration),
                _ => throw new InvalidOperationException($"Unsupported LLM type: {llmType}")
            };
        });
    }
}