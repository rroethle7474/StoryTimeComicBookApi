using StoryTimeComicBookApi.Data.Enums;

namespace StoryTimeComicBookApi.Models.Requests;

public class AssetCreateRequest
{
    public string ComicBookId { get; set; } = string.Empty;
    public string AssetType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? FullStoryText { get; set; }
    public string Status { get; set; } = "IN_PROGRESS";
    public int? PageNumber { get; set; }
} 