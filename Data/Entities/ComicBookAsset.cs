using StoryTimeComicBookApi.Data.Enums;
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
    public AssetType AssetType { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FilePath { get; set; } = string.Empty;
    
    public string? FullStoryText { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "IN_PROGRESS";
    
    public int? PageNumber { get; set; }
    
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ComicBookId")]
    public ComicBook ComicBook { get; set; } = null!;
} 