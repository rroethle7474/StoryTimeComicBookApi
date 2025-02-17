using Microsoft.EntityFrameworkCore;
using StoryTimeComicBookApi.Data.Entities;

namespace StoryTimeComicBookApi.Data
{
    public class ComicBookDataContext : DbContext
    {
        public ComicBookDataContext(DbContextOptions<ComicBookDataContext> options) : base(options)
        {
        }

        public DbSet<ComicBook> ComicBooks { get; set; } // DbSet for ComicBook entity
        public DbSet<Scene> Scenes { get; set; }       // DbSet for Scene entity

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and any specific model configurations here, if needed.
            // Example: modelBuilder.Entity<Scene>().HasOne(s => s.ComicBook).WithMany(cb => cb.Scenes).HasForeignKey(s => s.ComicBookId);
        }
    }
}
