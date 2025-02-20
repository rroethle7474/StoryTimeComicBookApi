using System.ComponentModel.DataAnnotations;

namespace StoryTimeComicBookApi.Data.Entities;

public class ReplicateModelVersion
{
    [Key]
    public Guid ReplicateVersionId { get; set; }
    
    public Guid ReplicateModelId { get; set; }
    public Guid VoiceModelId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string VersionIdentifier { get; set; } = string.Empty;
    
    public string? TrainingOutput { get; set; }
    public DateTime TrainedAt { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
    
    // Navigation properties
    public ReplicateModel ReplicateModel { get; set; } = null!;
    public VoiceModel VoiceModel { get; set; } = null!;
} 