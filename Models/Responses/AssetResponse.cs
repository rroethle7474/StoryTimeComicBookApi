namespace StoryTimeComicBookApi.Models.Responses;

public class AssetResponse
{
    public string AssetId { get; set; } = string.Empty;
    public string ComicBookId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public DateTime CreatedAt { get; set; }
} 