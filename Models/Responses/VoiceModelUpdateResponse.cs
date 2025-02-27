namespace StoryTimeComicBookApi.Models.Responses
{
    public class VoiceModelUpdateResponse
    {
        public string VoiceModelId { get; set; }
        public string VoiceModelName { get; set; }
        public string VoiceModelDescription { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime TrainingDate { get; set; }
        public string? ReplicateModelId { get; set; }
        public string? ReplicateModelName { get; set; }
    }
}
