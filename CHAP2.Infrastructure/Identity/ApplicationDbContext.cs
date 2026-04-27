using CHAP2.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CHAP2.Infrastructure.Identity;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Setlist> Setlists => Set<Setlist>();
    public DbSet<SetlistItem> SetlistItems => Set<SetlistItem>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Setlist>(builder =>
        {
            builder.ToTable("Setlists");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id).ValueGeneratedNever();
            builder.Property(s => s.OwnerId).IsRequired().HasMaxLength(450);
            builder.HasIndex(s => s.OwnerId);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(120);
            builder.Property(s => s.CreatedAt);
            builder.Property(s => s.UpdatedAt);
            builder.Property(s => s.IsWorkingDraft).HasDefaultValue(false);
            builder.HasIndex(s => new { s.OwnerId, s.IsWorkingDraft });

            builder.HasMany(s => s.Items)
                .WithOne()
                .HasForeignKey(i => i.SetlistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata
                .FindNavigation(nameof(Setlist.Items))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Ignore(s => s.DomainEvents);
        });

        modelBuilder.Entity<SetlistItem>(builder =>
        {
            builder.ToTable("SetlistItems");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id).ValueGeneratedNever();
            builder.Property(i => i.SetlistId).IsRequired();
            builder.Property(i => i.Position).IsRequired();
            builder.Property(i => i.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();

            builder.Property(i => i.ChorusId);
            builder.Property(i => i.BookId).HasMaxLength(64);
            builder.Property(i => i.BookName).HasMaxLength(120);
            builder.Property(i => i.Chapter);
            builder.Property(i => i.Verse);
            builder.Property(i => i.VerseText);
            builder.Property(i => i.VerseRef).HasMaxLength(200);

            builder.HasIndex(i => new { i.SetlistId, i.Position }).IsUnique();
        });

        modelBuilder.Entity<UserPreferences>(builder =>
        {
            builder.ToTable("UserPreferences");
            builder.HasKey(p => p.UserId);
            builder.Property(p => p.UserId).HasMaxLength(450);
            builder.Property(p => p.Theme).HasConversion<string>().HasMaxLength(32);
            builder.Property(p => p.DefaultSearchScope).HasConversion<string>().HasMaxLength(32);
            builder.Property(p => p.Language).HasConversion<string>().HasMaxLength(16);
            builder.Property(p => p.UpdatedAt);

            builder.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
