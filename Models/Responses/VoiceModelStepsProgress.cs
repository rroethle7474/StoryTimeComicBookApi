namespace StoryTimeComicBookApi.Models.Responses
{
    public class VoiceModelStepsProgress
    {
        public string VoiceModelId { get; set; } = string.Empty;
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public List<StepWithRecordingResponse> Steps { get; set; } = new();
    }
}
