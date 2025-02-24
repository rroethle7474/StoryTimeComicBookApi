// Services/Clients/AnthropicApiClient.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StoryTimeComicBookApi.Services.Clients.Interfaces;

namespace StoryTimeComicBookApi.Services.Clients;

public class AnthropicApiClient : BaseLlmClient
{
    private readonly string _apiKey;
    private readonly string _modelName;
    private const string BASE_URL = "https://api.anthropic.com/v1/messages";

    public AnthropicApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : base(httpClientFactory, configuration)
    {
        _apiKey = configuration["AI:Anthropic:ApiKey"] ??
            throw new InvalidOperationException("Anthropic API key is not configured");
        _modelName = configuration["AI:Anthropic:ModelName"] ?? "claude-3-opus-20240229";
    }

    public override async IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt)
    {
        string[] paragraphs = Array.Empty<string>();

        try
        {
            var request = new
            {
                model = _modelName,
                messages = new[]
                {
                new { role = "user", content = prompt }
            },
                max_tokens = 4000,
                temperature = 0.7
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var client = _httpClientFactory.CreateClient("AnthropicApi");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await client.PostAsync(BASE_URL, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);

            var content_block = document.RootElement.GetProperty("content");
            if (content_block.GetArrayLength() > 0)
            {
                var messageContent = content_block[0].GetProperty("text").GetString();

                if (!string.IsNullOrEmpty(messageContent))
                {
                    // Split content into paragraphs for streaming
                    paragraphs = messageContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error generating content with Anthropic API", ex);
        }

        // Now yield return outside of the try-catch block
        foreach (var paragraph in paragraphs)
        {
            yield return paragraph + "\n\n";
        }
    }
}