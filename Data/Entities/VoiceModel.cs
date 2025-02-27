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

    // Replace ActiveReplicateVersionId with direct reference to ReplicateModel
    public Guid? ReplicateModelId { get; set; }

    // Navigation property to ReplicateModel
    [ForeignKey("ReplicateModelId")]
    public ReplicateModel? ReplicateModel { get; set; }

    // Remove ModelVersions collection
    public ICollection<VoiceModelAudioSnippet> AudioSnippets { get; set; } = new List<VoiceModelAudioSnippet>();
}