using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities
{
    public class Scene
    {
        [Key]
        public Guid SceneId { get; set; } // UUID or int
        [Required]
        public Guid ComicBookId { get; set; } // Foreign Key to ComicBook
        [Required]
        public int SceneOrder { get; set; }
        [MaxLength(255)]
        public string? ImagePath { get; set; }
        public string? UserDescription { get; set; }
        public string? AiGeneratedStory { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        [ForeignKey("ComicBookId")] // Explicitly define foreign key relationship
        public ComicBook ComicBook { get; set; } // Navigation property to ComicBook
    }
}
