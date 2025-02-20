using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities;

public class ReplicateModel
{
    [Key]
    public Guid ReplicateModelId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ModelName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string ModelOwner { get; set; } = string.Empty;
    
    public string? ModelDescription { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ReplicateModelIdentifier { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation property
    public ICollection<ReplicateModelVersion> ModelVersions { get; set; } = new List<ReplicateModelVersion>();
} 