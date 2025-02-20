using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities
{
    public class AudioSnippet
    {
        [Key]
        public Guid AudioSnippetId { get; set; } // UUID or int
        [Required]
        [MaxLength(255)]
        public string AudioFilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Navigation property for many-to-many relationship with VoiceModel
        public ICollection<VoiceModelAudioSnippet> VoiceModels { get; set; } = new List<VoiceModelAudioSnippet>();
        // Add other properties as needed
    }
}
