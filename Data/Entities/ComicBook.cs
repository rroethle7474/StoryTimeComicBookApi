using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities
{
    public class ComicBook
    {
        [Key]
        public Guid ComicBookId { get; set; } // Assuming UUIDs, if using SERIAL, use int and [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AdditionalDetails { get; set; }
        [MaxLength(255)]
        public string? FinalComicBookPath { get; set; }
        [MaxLength(50)]
        public string GenerationStatus { get; set; } = "Pending";
        public bool IsCompleted { get; set; } // Add this line
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Scene> Scenes { get; set; } = new List<Scene>(); // Navigation property for Scenes
        public ICollection<ComicBookAsset> Assets { get; set; } = new List<ComicBookAsset>();
    }
}
