namespace StoryTimeComicBookApi.Models.Requests;

public class ComicBookUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? IsCompleted { get; set; }
} 