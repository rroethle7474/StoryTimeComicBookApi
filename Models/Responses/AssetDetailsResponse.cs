namespace StoryTimeComicBookApi.Models.Responses
{
    public class AssetDetailsResponse
    {
        public string AssetId { get; set; } = string.Empty;
        public string ComicBookId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? FullStoryText { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
