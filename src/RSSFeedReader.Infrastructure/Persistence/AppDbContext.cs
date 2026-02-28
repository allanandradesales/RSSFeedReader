using Microsoft.EntityFrameworkCore;
using RSSFeedReader.Domain.Entities;

namespace RSSFeedReader.Infrastructure.Persistence;

/// <summary>EF Core database context for the RSS feed reader.</summary>
public sealed class AppDbContext : DbContext
{
    /// <summary>Initializes a new instance of <see cref="AppDbContext"/>.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Gets the feeds table.</summary>
    public DbSet<Feed> Feeds => Set<Feed>();

    /// <summary>Gets the articles table.</summary>
    public DbSet<Article> Articles => Set<Article>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Feed>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Url).IsRequired().HasMaxLength(2048);
            entity.HasIndex(f => f.Url).IsUnique();
            entity.Property(f => f.Title).IsRequired().HasMaxLength(512);
            entity.Property(f => f.CreatedAt).IsRequired();

            entity.HasMany(f => f.Articles)
                  .WithOne(a => a.Feed)
                  .HasForeignKey(a => a.FeedId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.FeedGuid).IsRequired().HasMaxLength(1024);
            entity.HasIndex(a => a.FeedGuid).IsUnique();
            entity.Property(a => a.Title).IsRequired().HasMaxLength(1024);
            entity.Property(a => a.Summary).HasMaxLength(4096);
            entity.Property(a => a.OriginalUrl).IsRequired().HasMaxLength(2048);
            entity.Property(a => a.PublishedAt).IsRequired();
            entity.Property(a => a.FetchedAt).IsRequired();
            entity.Property(a => a.IsRead).IsRequired().HasDefaultValue(false);

            entity.HasIndex(a => new { a.FeedId, a.PublishedAt });
        });
    }
}
