namespace StoryTimeComicBookApi.Models.Responses
{
    public class ComicBookStatusResponse
    {
        public string Status { get; set; } = "Pending"; // Default to Pending
        public int Progress { get; set; } = 0; // Percentage (0-100)
        public string? EstimatedTimeRemaining { get; set; } // Example: "2 minutes remaining"
        public string? Message { get; set; } // Any additional messages
        public string? AssetId { get; set; } // Reference to the comic book asset
    }
}
