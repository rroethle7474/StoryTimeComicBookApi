namespace StoryTimeComicBookApi.Models.Responses;

public class StartRecordingResponse
{
    public string RecordingSessionId { get; set; } = string.Empty;
    public string Message { get; set; } = "Recording session started.";
} 