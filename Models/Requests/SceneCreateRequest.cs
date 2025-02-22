namespace StoryTimeComicBookApi.Models.Requests;

public class SceneCreateRequest
{
    public string ComicBookId { get; set; } = string.Empty;
    public int SceneOrder { get; set; }
    public string? ImagePath { get; set; }
    public string? StyledImagePath { get; set; }
    public string? UserDescription { get; set; }
    public string? DialogueText { get; set; }
    public string? TransitionNotes { get; set; }
} 