namespace StoryTimeComicBookApi.Models.Responses;

public class ComicBookUpdateResponse
{
    public string ComicBookId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AdditionalDetails { get; set; }
    public string? FinalComicBookPath { get; set; }
    public string GenerationStatus { get; set; } = "Pending";
    public bool IsCompleted { get; set; }
} 