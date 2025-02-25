using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoryTimeComicBookApi.Data.Entities;

public class VoiceModel
{
    [Key]
    public Guid VoiceModelId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string VoiceModelName { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string VoiceModelDescription { get; set; } = string.Empty;

    [MaxLength(255)]
    public string HuggingFaceModelName { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";
    
    public DateTime TrainingDate { get; set; }
    
    public Guid? ActiveReplicateVersionId { get; set; }
    
    // Navigation properties
    [ForeignKey("ActiveReplicateVersionId")]
    public ReplicateModelVersion? ActiveVersion { get; set; }
    
    public ICollection<ReplicateModelVersion> ModelVersions { get; set; } = new List<ReplicateModelVersion>();
    public ICollection<VoiceModelAudioSnippet> AudioSnippets { get; set; } = new List<VoiceModelAudioSnippet>();
}