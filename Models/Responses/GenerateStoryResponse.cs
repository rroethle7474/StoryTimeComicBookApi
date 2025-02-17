namespace StoryTimeComicBookApi.Models.Responses;

public class GenerateStoryResponse
{
    public string SceneId { get; set; } = string.Empty;
    public string StoryTextChunk { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
} 