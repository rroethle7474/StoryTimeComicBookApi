namespace StoryTimeComicBookApi.Models.Responses;

public class ComicBookGetResponse
{
    public string ComicBookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<SceneGetResponse> Scenes { get; set; } = new();
} 