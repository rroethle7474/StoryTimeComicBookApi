namespace StoryTimeComicBookApi.Models.Responses
{
    public class StepResponse
    {
        public string StepId { get; set; } = string.Empty;
        public int StepNumber { get; set; }
        public string TranscriptText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
