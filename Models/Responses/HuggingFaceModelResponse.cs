namespace StoryTimeComicBookApi.Models.Responses
{
    public class HuggingFaceModelResponse
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }

        // Additional info that might be useful
        public bool IsLinkedToVoiceModel { get; set; }
        public string? LinkedVoiceModelId { get; set; }
    }
}