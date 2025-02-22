namespace StoryTimeComicBookApi.Models.Requests;

public class ComicBookUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? AdditionalDetails { get; set; }
    public string? FinalComicBookPath { get; set; }
    public string? GenerationStatus { get; set; }
    public bool? IsCompleted { get; set; }
} 