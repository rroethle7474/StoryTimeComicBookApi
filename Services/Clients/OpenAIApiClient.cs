// Services/Clients/OpenAIApiClient.cs
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StoryTimeComicBookApi.Services.Clients.Interfaces;

namespace StoryTimeComicBookApi.Services.Clients;

public class OpenAIApiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private const string BASE_URL = "https://api.openai.com/v1/chat/completions";

    public OpenAIApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _apiKey = configuration["AI:OpenAI:ApiKey"] ??
            throw new InvalidOperationException("OpenAI API key is not configured");
        _modelName = configuration["AI:OpenAI:ModelName"] ?? "gpt-3.5-turbo";
        _httpClient = httpClientFactory.CreateClient("OpenAIApi");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt)
    {
        string[] sentences = Array.Empty<string>();

        try
        {
            var request = new
            {
                model = _modelName,
                messages = new[]
                {
                new { role = "system", content = "You are a creative assistant that helps users generate story content." },
                new { role = "user", content = prompt }
            },
                temperature = 0.7,
                max_tokens = 1500,
                stream = false
            };

            var jsonRequest = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(BASE_URL, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonResponse);

            var choices = document.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();

                if (!string.IsNullOrEmpty(messageContent))
                {
                    // Split content into paragraphs or reasonable chunks
                    sentences = messageContent.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error generating content with OpenAI API", ex);
        }

        // Now yield return outside of the try-catch block
        foreach (var sentence in sentences)
        {
            yield return sentence.Trim() + ". ";
        }
    }
}