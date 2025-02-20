namespace StoryTimeComicBookApi.Models.Responses
{
    public class AudioSnippetResponse
    {
        public string AudioSnippetId { get; set; } = string.Empty;
        public string AudioFilePath { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }
}
