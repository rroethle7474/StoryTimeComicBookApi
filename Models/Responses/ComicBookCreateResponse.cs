namespace StoryTimeComicBookApi.Models.Responses;

public class ComicBookCreateResponse
{
    public string ComicBookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AdditionalDetails { get; set; }
    public string GenerationStatus { get; set; } = "Pending";
} 