using System.Text;
using System.Text.Json;
using StoryTimeComicBookApi.Models.Gemini;
using StoryTimeComicBookApi.Services.Clients.Interfaces;

namespace StoryTimeComicBookApi.Services.Clients;

public class GeminiApiClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BASE_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

    public GeminiApiClient(string apiKey)
    {
        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    public async IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt)
    {
        var request = new GeminiRequest
        {
            Contents = new List<Content>
            {
                new Content
                {
                    Parts = new List<Part>
                    {
                        new Part { Text = prompt }
                    }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = 0.7,
                CandidateCount = 1,
                MaxOutputTokens = 800
            }
        };

        var url = $"{BASE_URL}?key={_apiKey}";
        var jsonRequest = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        string[] sentences = Array.Empty<string>();

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

            if (geminiResponse?.Candidates != null && geminiResponse.Candidates.Any())
            {
                var text = geminiResponse.Candidates[0].Content.Parts[0].Text;
                sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error generating content with Gemini API", ex);
        }

        foreach (var sentence in sentences)
        {
            yield return sentence.Trim() + ". ";
        }
    }
} 