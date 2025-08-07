using Microsoft.EntityFrameworkCore;
using UrlShortenerSystem.Models;

namespace UrlShortenerSystem.Data
{
    public class UrlShortenerContext : DbContext
    {
        public UrlShortenerContext(DbContextOptions<UrlShortenerContext> options) : base(options)
        {
        }

        public DbSet<ShortUrl> ShortUrls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ShortUrl>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedNever();

                entity.Property(e => e.OriginalUrl)
                    .IsRequired()
                    .HasMaxLength(2048);

                entity.Property(e => e.ShortCode)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.HasIndex(e => e.ShortCode)
                    .IsUnique();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.Clicks);
            });
        }
    }
}
