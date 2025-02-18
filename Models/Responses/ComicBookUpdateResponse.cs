namespace StoryTimeComicBookApi.Models.Responses;

public class ComicBookUpdateResponse
{
    public string ComicBookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
} 