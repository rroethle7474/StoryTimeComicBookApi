namespace StoryTimeComicBookApi.Services.Clients.Interfaces;

public interface ILlmClient
{
    IAsyncEnumerable<string> GenerateContentStreamAsync(string prompt);
} 