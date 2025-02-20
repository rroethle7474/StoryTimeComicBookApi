using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities
{
    public class VoiceRecordingStep
    {
        [Key]
        public Guid StepId { get; set; }

        [Required]
        public int StepNumber { get; set; }

        [Required]
        public string TranscriptText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation property
        public ICollection<VoiceModelAudioSnippet> VoiceModelAudioSnippets { get; set; } = new List<VoiceModelAudioSnippet>();
    }
}
