namespace StoryTimeComicBookApi.Models.Responses;

public class VoiceModelListResponse
{
    public string VoiceModelId { get; set; } = string.Empty;
    public string VoiceModelName { get; set; } = string.Empty;
    public string VoiceModelDescription { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime TrainingDate { get; set; }
} 