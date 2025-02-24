using StoryTimeComicBookApi.Data.Enums;

namespace StoryTimeComicBookApi.Models.Requests;

public class AssetUpdateRequest
{
    public string? AssetType { get; set; }
    public string? FilePath { get; set; }
    public string? FullStoryText { get; set; }
    public string? Status { get; set; }
    public int? PageNumber { get; set; }
} 