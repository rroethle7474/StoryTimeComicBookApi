namespace StoryTimeComicBookApi.Models.Responses;

public class ComicBookDeleteResponse
{
    public string ComicBookId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
} 