using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Scoring.Application.Abstractions;
using Scoring.Domain.Abstractions;
using Scoring.Domain.CreditProfiles;

namespace Scoring.Infrastructure.Persistence;

public sealed class ScoringDbContext(DbContextOptions<ScoringDbContext> options) : DbContext(options), IUnitOfWork
{
    public const string Schema = "scoring";

    public DbSet<CreditProfile> CreditProfiles => Set<CreditProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfiguration(new CreditProfileConfiguration());
        modelBuilder.ApplyConfiguration(new ScoreSnapshotConfiguration());

        // Inbox = consumer idempotency; outbox = reliable publishing from consumers.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}

internal sealed class CreditProfileConfiguration : IEntityTypeConfiguration<CreditProfile>
{
    public void Configure(EntityTypeBuilder<CreditProfile> builder)
    {
        builder.ToTable("credit_profiles", t => t.HasCheckConstraint("ck_credit_profiles_score_range", "score BETWEEN 300 AND 850"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("user_id").ValueGeneratedNever();

        builder.Property(p => p.DisplayName).HasMaxLength(80).IsRequired();
        builder.Property(p => p.Band).HasConversion<string>().HasMaxLength(16);
        builder.Property(p => p.TotalInflow).HasPrecision(18, 2);
        builder.Property(p => p.TotalOutflow).HasPrecision(18, 2);
        builder.Property(p => p.LastKnownBalance).HasPrecision(18, 2);
        builder.Property(p => p.PeakBalance).HasPrecision(18, 2);

        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(p => p.History)
            .WithOne()
            .HasForeignKey(s => s.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class ScoreSnapshotConfiguration : IEntityTypeConfiguration<ScoreSnapshot>
{
    public void Configure(EntityTypeBuilder<ScoreSnapshot> builder)
    {
        builder.ToTable("score_snapshots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Reason).HasMaxLength(120).IsRequired();
        builder.HasIndex(s => new { s.ProfileId, s.RecordedAtUtc });
    }
}

internal sealed class CreditProfileRepository(ScoringDbContext db) : ICreditProfileRepository
{
    public Task<CreditProfile?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        db.CreditProfiles
            .Include(p => p.History)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

    public void Add(CreditProfile profile) => db.CreditProfiles.Add(profile);
}
