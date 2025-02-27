namespace StoryTimeComicBookApi.Models.Responses;

public class ReplicateModelListResponse
{
    public string? ReplicateModelId { get; set; }
    public string? ReplicateModelName { get; set; }
    public bool IsCurrentlySelected { get; set; } = false;
}