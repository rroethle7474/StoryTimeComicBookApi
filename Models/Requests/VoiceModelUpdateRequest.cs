namespace StoryTimeComicBookApi.Models.Requests
{
    public class VoiceModelUpdateRequest
    {
        public string? VoiceModelName { get; set; }
        public string? VoiceModelDescription { get; set; }
        public bool? IsCompleted { get; set; }
    }
}
