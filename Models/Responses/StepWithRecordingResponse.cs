namespace StoryTimeComicBookApi.Models.Responses
{
    public class StepWithRecordingResponse
    {
        public string StepId { get; set; } = string.Empty;
        public int StepNumber { get; set; }
        public string TranscriptText { get; set; } = string.Empty;
        public AudioRecordingDetails? Recording { get; set; }
    }
}
