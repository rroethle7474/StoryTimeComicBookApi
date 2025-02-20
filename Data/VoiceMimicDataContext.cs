using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data.Entities;

namespace StoryTimeComicBookApi.Data
{
    public class VoiceMimicDataContext : DbContext
    {
        public VoiceMimicDataContext(DbContextOptions<VoiceMimicDataContext> options) : base(options)
        {
        }

        public DbSet<AudioSnippet> AudioSnippets { get; set; } // DbSet for AudioSnippet
        public DbSet<VoiceModel> VoiceModels { get; set; }     // DbSet for VoiceModel

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AudioSnippet>().ToTable("AudioSnippets", "voice_mimic_schema");
            modelBuilder.Entity<VoiceModel>().ToTable("VoiceModels", "voice_mimic_schema");
        }
    }
}
