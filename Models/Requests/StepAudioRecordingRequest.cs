namespace StoryTimeComicBookApi.Models.Requests
{
    public class StepAudioRecordingRequest
    {
        public string VoiceModelId { get; set; } = string.Empty;
        public string StepId { get; set; } = string.Empty;
        public IFormFile AudioFile { get; set; } = null!;
    }
}
