using StoryTimeComicBookApi.Services.Clients.Interfaces;
using StoryTimeComicBookApi.Services.Interfaces;

namespace StoryTimeComicBookApi.Services.AI;

public class AiStoryGenerator : IAiStoryGenerator
{
    private readonly ILlmClient _client;
    private readonly ILogger<AiStoryGenerator> _logger;

    public AiStoryGenerator(
        ILlmClient client,
        ILogger<AiStoryGenerator> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> GenerateStoryAsync(string sceneDescription)
    {
        var prompt = $"Create a short story based on this scene description: {sceneDescription}";
        var buffer = new List<string>();
        
        try
        {
            await foreach (var chunk in _client.GenerateContentStreamAsync(prompt))
            {
                buffer.Add(chunk);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating story with LLM");
            throw;
        }

        foreach (var chunk in buffer)
        {
            yield return chunk;
        }
    }
} 