using StoryTimeComicBookApi.Data.Enums;

namespace StoryTimeComicBookApi.Models.Responses;

public class AssetResponse
{
    public string AssetId { get; set; } = string.Empty;
    public string ComicBookId { get; set; } = string.Empty;
    public string AssetType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? FullStoryText { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public int? PageNumber { get; set; }
    public DateTime CreatedAt { get; set; }
} 