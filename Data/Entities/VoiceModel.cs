using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities
{
    public class VoiceModel
    {
        [Key]
        public Guid VoiceModelId { get; set; } // UUID or int
        [MaxLength(255)]
        public string? VoiceModelName { get; set; }
        public DateTime TrainingDate { get; set; }
        // Add other properties as needed
    }
}
