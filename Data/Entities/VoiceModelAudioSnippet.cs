using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StoryTimeComicBookApi.Data.Entities;

public class VoiceModelAudioSnippet
{
    public Guid VoiceModelId { get; set; }
    public Guid AudioSnippetId { get; set; }
    public Guid? StepId { get; set; }
    public DateTime AddedAt { get; set; }

    // Navigation properties
    public VoiceModel VoiceModel { get; set; } = null!;
    public AudioSnippet AudioSnippet { get; set; } = null!;

    [ForeignKey("StepId")]
    public VoiceRecordingStep? Step { get; set; }
}