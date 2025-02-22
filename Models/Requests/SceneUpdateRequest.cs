namespace StoryTimeComicBookApi.Models.Requests;

public class SceneUpdateRequest
{
    public string? ImagePath { get; set; }
    public string? StyledImagePath { get; set; }
    public string? UserDescription { get; set; }
    public string? DialogueText { get; set; }
    public string? TransitionNotes { get; set; }
    public int? SceneOrder { get; set; }
    public string? AiGeneratedStory { get; set; }
} 