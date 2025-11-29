using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.DbContext;

public partial class PerfumeTrackerContext : IdentityDbContext<PerfumeIdentityUser, PerfumeIdentityRole, Guid> {
	public ITenantProvider? TenantProvider { get; set; }

	public PerfumeTrackerContext(ITenantProvider tenantProvider) {
		TenantProvider = tenantProvider;
	}

	public PerfumeTrackerContext(DbContextOptions<PerfumeTrackerContext> options, ITenantProvider tenantProvider)
		: base(options) {
		TenantProvider = tenantProvider;
	}
	public virtual DbSet<UserStreak> UserStreaks { get; set; }
	public virtual DbSet<Perfume> Perfumes { get; set; }
	public virtual DbSet<PerfumeRandoms> PerfumeRandoms { get; set; }
	public virtual DbSet<PerfumeTag> PerfumeTags { get; set; }
	public virtual DbSet<PerfumeEvent> PerfumeEvents { get; set; }
	public virtual DbSet<PerfumeRating> PerfumeRatings { get; set; }
	public virtual DbSet<Recommendation> Recommendations { get; set; }
	public virtual DbSet<Tag> Tags { get; set; }
	public virtual DbSet<Achievement> Achievements { get; set; }
	public virtual DbSet<UserAchievement> UserAchievements { get; set; }
	public virtual DbSet<UserProfile> UserProfiles { get; set; }
	public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }
	public virtual DbSet<Mission> Missions { get; set; }
	public virtual DbSet<UserMission> UserMissions { get; set; }
	public virtual DbSet<Invite> Invites { get; set; }
	protected override void OnModelCreating(ModelBuilder builder) {

		builder.Entity<OutboxMessage>(entity => {
			entity.HasKey(e => e.Id).HasName("OutboxMessage_pkey");
			entity.ToTable("OutboxMessage");
			entity.HasIndex(o => new { o.ProcessedAt, o.CreatedAt })
				.HasDatabaseName("IX_Outbox_ProcessedAt_CreatedAt");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});
		builder.Entity<Achievement>(entity => {
			entity.HasKey(e => e.Id).HasName("Achievement_pkey");
			entity.ToTable("Achievement");
			entity.HasQueryFilter(x => !x.IsDeleted);
		});
		builder.Entity<UserAchievement>(entity => {
			entity.HasKey(e => e.Id).HasName("UserAchievement_pkey");
			entity.ToTable("UserAchievement");

			entity.HasOne(e => e.Achievement)
				.WithMany()
				.HasForeignKey(e => e.AchievementId)
				.HasConstraintName("UserAchievement_AchievementId_fkey");

			entity.HasOne(e => e.UserProfile)
				.WithMany(e => e.UserAchievements)
				.HasForeignKey(e => e.UserId)
				.OnDelete(DeleteBehavior.Restrict)
				.HasConstraintName("UserAchievement_UserId_fkey");

			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<UserProfile>(entity => {
			entity.HasKey(e => e.Id).HasName("UserProfile_pkey");
			entity.ToTable("UserProfile");
			entity.Property(e => e.Timezone).HasDefaultValue("UTC");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.Id == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<UserStreak>(entity => {
			entity.HasKey(e => e.Id).HasName("UserStreak_pkey");
			entity.ToTable("UserStreak");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<Perfume>(entity => {
			entity.HasKey(e => e.Id).HasName("Perfume_pkey");

			entity.ToTable("Perfume");
			entity.Property(e => e.Autumn)
				.HasDefaultValue(true);
			entity.Property(e => e.ImageObjectKeyNew);
			entity.Property(e => e.Ml)
				.HasDefaultValue(2);
			entity.Property(e => e.Spring)
				.HasDefaultValue(true);
			entity.Property(e => e.Summer)
				.HasDefaultValue(true);
			entity.Property(e => e.Winter)
				.HasDefaultValue(true);
			entity.HasGeneratedTsVectorColumn(
				p => p.FullText,
				"simple",
				p => new { p.PerfumeName, p.House })
				.HasIndex(p => p.FullText)
				.HasMethod("GIN");
			entity
				.HasIndex(p => new { p.UserId, p.House, p.PerfumeName })
				.HasFilter(@"""IsDeleted"" = FALSE")
				.IsUnique();
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<PerfumeRandoms>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeRandom_pkey");

			entity.ToTable("PerfumeRandom");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeRandoms)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeRandom_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<PerfumeTag>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeTag_pkey");

			entity.ToTable("PerfumeTag");

			entity.HasOne(d => d.Perfume).WithMany(p => p.PerfumeTags)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeTag_perfumeId_fkey");

			entity.HasOne(d => d.Tag).WithMany(p => p.PerfumeTags)
				.HasForeignKey(d => d.TagId)
				.OnDelete(DeleteBehavior.Restrict)
				.HasConstraintName("PerfumeTag_tagId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<PerfumeEvent>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeEvent_pkey");

			entity.ToTable("PerfumeEvent");

			entity.Property(e => e.SequenceNumber)
				.HasDefaultValueSql("nextval('\"PerfumeEventSequence\"')")
				.ValueGeneratedOnAdd();

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeEvents)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeEvent_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<PerfumeRating>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeRating_pkey");

			entity.ToTable("PerfumeRating");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeRatings)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeRating_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<Recommendation>(entity => {
			entity.HasKey(e => e.Id).HasName("Recommendation_pkey");

			entity.ToTable("Recommendation");

			entity.Property(e => e.CreatedAt)
				.HasDefaultValueSql("CURRENT_TIMESTAMP");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<Tag>(entity => {
			entity.HasKey(e => e.Id).HasName("Tag_pkey");

			entity.ToTable("Tag");

			entity.HasIndex(e => new { e.UserId, e.TagName }, "Tag_tag_key")
				.HasFilter(@"""IsDeleted"" = FALSE")
				.IsUnique();
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<Mission>(entity => {
			entity.HasKey(e => e.Id).HasName("Mission_pkey");
			entity.ToTable("Mission");
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		builder.Entity<UserMission>(entity => {
			entity.HasKey(e => e.Id).HasName("UserMission_pkey");
			entity.ToTable("UserMission");

			entity.HasOne(e => e.User)
				.WithMany(e => e.UserMissions)
				.HasForeignKey(e => e.UserId)
				.HasConstraintName("UserMission_UserId_fkey");

			entity.HasOne(e => e.Mission)
				.WithMany(e => e.UserMissions)
				.HasForeignKey(e => e.MissionId)
				.HasConstraintName("UserMission_MissionId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && (TenantProvider == null || x.UserId == TenantProvider.GetCurrentUserId()));
		});

		builder.Entity<Invite>(entity => {
			entity.HasKey(e => e.Id).HasName("Invite_pkey");
			entity.ToTable("Invite");
			entity.HasQueryFilter(x => !x.IsDeleted && !x.IsUsed);
		});

		base.OnModelCreating(builder);
	}
}