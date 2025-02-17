using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities
{
    public class AudioSnippet
    {
        [Key]
        public Guid AudioSnippetId { get; set; } // UUID or int
        [Required]
        [MaxLength(255)]
        public string AudioFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Add other properties as needed
    }
}
