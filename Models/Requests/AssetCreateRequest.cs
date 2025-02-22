namespace StoryTimeComicBookApi.Models.Requests;

public class AssetCreateRequest
{
    public string ComicBookId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
} 