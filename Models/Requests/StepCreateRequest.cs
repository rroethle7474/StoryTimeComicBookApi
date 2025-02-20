namespace StoryTimeComicBookApi.Models.Requests
{
    public class StepCreateRequest
    {
        public int StepNumber { get; set; }
        public string TranscriptText { get; set; } = string.Empty;
    }
}
