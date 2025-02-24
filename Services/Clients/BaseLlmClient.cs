// Services/Clients/BaseLlmClient.cs
using StoryTimeComicBookApi.Services.Clients.Interfaces;

namespace StoryTimeComicBookApi.Services.Clients;

public abstract class BaseLlmClient : ILlmClient
{
    protected readonly IConfiguration _configuration;
    protected readonly IHttpClientFactory _httpClientFactory;

    protected BaseLlmClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public abstract IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt);
}