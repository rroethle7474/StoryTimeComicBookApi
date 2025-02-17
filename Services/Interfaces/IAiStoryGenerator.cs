namespace StoryTimeComicBookApi.Services.Interfaces;

public interface IAiStoryGenerator
{
    IAsyncEnumerable<string> GenerateStoryAsync(string sceneDescription);
} 