namespace StoryTimeComicBookApi.Models.Requests;

public class AssetUpdateRequest
{
    public string? AssetType { get; set; }
    public string? FilePath { get; set; }
    public int? PageNumber { get; set; }
} 