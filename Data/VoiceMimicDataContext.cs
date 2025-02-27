using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data.Entities;

namespace StoryTimeComicBookApi.Data;

public class VoiceMimicDataContext : DbContext
{
    public VoiceMimicDataContext(DbContextOptions<VoiceMimicDataContext> options) : base(options)
    {
    }

    public DbSet<AudioSnippet> AudioSnippets { get; set; }
    public DbSet<VoiceModel> VoiceModels { get; set; }
    public DbSet<ReplicateModel> ReplicateModels { get; set; }
    // Remove: public DbSet<ReplicateModelVersion> ReplicateModelVersions { get; set; }
    public DbSet<VoiceModelAudioSnippet> VoiceModelAudioSnippets { get; set; }
    public DbSet<VoiceRecordingStep> VoiceRecordingSteps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("voice_mimic_schema");

        // Configure VoiceModelAudioSnippet junction table
        modelBuilder.Entity<VoiceModelAudioSnippet>()
            .HasKey(v => new { v.VoiceModelId, v.AudioSnippetId });

        modelBuilder.Entity<VoiceModelAudioSnippet>()
            .HasOne(v => v.VoiceModel)
            .WithMany(vm => vm.AudioSnippets)
            .HasForeignKey(v => v.VoiceModelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VoiceModelAudioSnippet>()
            .HasOne(v => v.AudioSnippet)
            .WithMany(a => a.VoiceModels)
            .HasForeignKey(v => v.AudioSnippetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure VoiceRecordingStep relationships
        modelBuilder.Entity<VoiceModelAudioSnippet>()
            .HasOne(v => v.Step)
            .WithMany(s => s.VoiceModelAudioSnippets)
            .HasForeignKey(v => v.StepId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VoiceRecordingStep>()
            .HasIndex(s => s.StepNumber)
            .IsUnique();

        // Configure unique constraint for VoiceModel-Step combination
        modelBuilder.Entity<VoiceModelAudioSnippet>()
            .HasIndex(v => new { v.VoiceModelId, v.StepId })
            .IsUnique()
            .HasFilter("\"StepId\" IS NOT NULL");

        // Remove ReplicateModelVersion relationships
        // Configure new relationship between VoiceModel and ReplicateModel
        modelBuilder.Entity<VoiceModel>()
            .HasOne(v => v.ReplicateModel)
            .WithMany(r => r.VoiceModels)
            .HasForeignKey(v => v.ReplicateModelId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Status columns
        modelBuilder.Entity<VoiceModel>()
            .Property(v => v.Status)
            .HasMaxLength(20)
            .HasDefaultValue("pending");

        // Configure unique constraints
        modelBuilder.Entity<ReplicateModel>()
            .HasIndex(rm => rm.ReplicateModelIdentifier)
            .IsUnique();

        // Add index for new foreign key
        modelBuilder.Entity<VoiceModel>()
            .HasIndex(v => v.ReplicateModelId);
    }
}