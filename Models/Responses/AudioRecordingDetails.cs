namespace StoryTimeComicBookApi.Models.Responses
{
    public class AudioRecordingDetails
    {
        public string AudioSnippetId { get; set; } = string.Empty;
        public string AudioFilePath { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
    }
}
