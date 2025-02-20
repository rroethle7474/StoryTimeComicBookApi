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
    public DbSet<ReplicateModelVersion> ReplicateModelVersions { get; set; }
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

        // Configure ReplicateModelVersion relationships
        modelBuilder.Entity<ReplicateModelVersion>()
            .HasOne(rv => rv.ReplicateModel)
            .WithMany(rm => rm.ModelVersions)
            .HasForeignKey(rv => rv.ReplicateModelId);

        modelBuilder.Entity<ReplicateModelVersion>()
            .HasOne(rv => rv.VoiceModel)
            .WithMany(vm => vm.ModelVersions)
            .HasForeignKey(rv => rv.VoiceModelId);

        // Configure Status columns
        modelBuilder.Entity<VoiceModel>()
            .Property(v => v.Status)
            .HasMaxLength(20)
            .HasDefaultValue("pending");

        modelBuilder.Entity<ReplicateModelVersion>()
            .Property(rv => rv.Status)
            .HasMaxLength(20)
            .HasDefaultValue("pending");

        // Configure unique constraints
        modelBuilder.Entity<ReplicateModel>()
            .HasIndex(rm => rm.ReplicateModelIdentifier)
            .IsUnique();

        modelBuilder.Entity<ReplicateModelVersion>()
            .HasIndex(rv => new { rv.VoiceModelId, rv.VersionIdentifier })
            .IsUnique();
    }
}