namespace StoryTimeComicBookApi.Models.Responses;

public class SceneCreateResponse
{
    public string SceneId { get; set; } = string.Empty;
    public int SceneOrder { get; set; }
    public string? ImagePath { get; set; }
    public string? UserDescription { get; set; }
} 