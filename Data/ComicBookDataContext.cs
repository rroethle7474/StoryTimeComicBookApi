using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StoryTimeComicBookApi.Data.Enums;

namespace StoryTimeComicBookApi.Data
{
    public class ComicBookDataContext : DbContext
    {
        public ComicBookDataContext(DbContextOptions<ComicBookDataContext> options) : base(options)
        {
        }

        public DbSet<ComicBook> ComicBooks { get; set; }
        public DbSet<Scene> Scenes { get; set; }
        public DbSet<ComicBookAsset> ComicBookAssets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Set schema
            modelBuilder.HasDefaultSchema("comic_book_schema");

            // Configure ComicBook
            modelBuilder.Entity<ComicBook>()
                .ToTable("ComicBooks");

            // Configure Scene
            modelBuilder.Entity<Scene>()
                .ToTable("Scenes")
                .HasOne(s => s.ComicBook)
                .WithMany(cb => cb.Scenes)
                .HasForeignKey(s => s.ComicBookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ComicBookAsset
            modelBuilder.Entity<ComicBookAsset>()
                .ToTable("ComicBookAssets")
                .HasOne(a => a.ComicBook)
                .WithMany(cb => cb.Assets)
                .HasForeignKey(a => a.ComicBookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create index for ComicBookAssets.ComicBookId
            modelBuilder.Entity<ComicBookAsset>()
                .HasIndex(a => a.ComicBookId)
                .HasDatabaseName("IX_ComicBookAssets_ComicBookId");
        }
    }
}
