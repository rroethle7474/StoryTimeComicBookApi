using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoryTimeComicBookApi.Data.Entities;

public class ComicBookAsset
{
    [Key]
    public Guid AssetId { get; set; }
    
    [Required]
    public Guid ComicBookId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AssetType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string FilePath { get; set; } = string.Empty;
    
    public int? PageNumber { get; set; }
    
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ComicBookId")]
    public ComicBook ComicBook { get; set; } = null!;
} 