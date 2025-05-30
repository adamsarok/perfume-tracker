namespace PerfumeTracker.Server.Models;

public partial class PerfumeTrackerContext : DbContext {
	public static readonly Guid DefaultUserID = new Guid("dc972e5e-1cd9-4105-92e9-f37bb9958dd9");
	private Guid currentUserID = DefaultUserID;
	public PerfumeTrackerContext() {
	}

	public PerfumeTrackerContext(DbContextOptions<PerfumeTrackerContext> options)
		: base(options) {
	}

	public virtual DbSet<Perfume> Perfumes { get; set; }
	public virtual DbSet<PerfumeRandoms> PerfumeRandoms { get; set; }
	public virtual DbSet<PerfumeTag> PerfumeTags { get; set; }
	public virtual DbSet<PerfumeEvent> PerfumeEvents { get; set; }
	public virtual DbSet<Recommendation> Recommendations { get; set; }
	public virtual DbSet<Tag> Tags { get; set; }
	public virtual DbSet<Achievement> Achievements { get; set; }
	public virtual DbSet<UserAchievement> UserAchievements { get; set; }
	public virtual DbSet<UserProfile> UserProfiles { get; set; }
	public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }
	public virtual DbSet<Mission> Missions { get; set; }
	public virtual DbSet<UserMission> UserMissions { get; set; }
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		modelBuilder.Entity<OutboxMessage>(entity => {
			entity.HasKey(e => e.Id).HasName("OutboxMessage_pkey");
			entity.ToTable("OutboxMessage");
			entity.HasIndex(o => new { o.ProcessedAt, o.CreatedAt })
				.HasDatabaseName("IX_Outbox_ProcessedAt_CreatedAt");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});
		modelBuilder.Entity<Achievement>(entity => {
			entity.HasKey(e => e.Id).HasName("Achievement_pkey");
			entity.ToTable("Achievement");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});
		modelBuilder.Entity<UserAchievement>(entity => {
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

			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<UserProfile>(entity => {
			entity.HasKey(e => e.Id).HasName("UserProfile_pkey");
			entity.ToTable("UserProfile");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<Perfume>(entity => {
			entity.HasKey(e => e.Id).HasName("Perfume_pkey");

			entity.ToTable("Perfume");
			entity.Property(e => e.Autumn)
				.HasDefaultValue(true);
			entity.Property(e => e.ImageObjectKey)
				.HasDefaultValueSql("''::text");
			entity.Property(e => e.Ml)
				.HasDefaultValue(2);
			entity.Property(e => e.Notes)
				.HasDefaultValueSql("''::text");
			entity.Property(e => e.Spring)
				.HasDefaultValue(true);
			entity.Property(e => e.Summer)
				.HasDefaultValue(true);
			entity.Property(e => e.Winter)
				.HasDefaultValue(true);
			entity.HasGeneratedTsVectorColumn(
				p => p.FullText,
				"english",
				p => new { p.PerfumeName, p.House, p.Notes, p.Rating })
				.HasIndex(p => p.FullText)
				.HasMethod("GIN");
			entity
				.HasIndex(p => new { p.House, p.PerfumeName }).IsUnique();
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<PerfumeRandoms>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeRandom_pkey");

			entity.ToTable("PerfumeRandom");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeRandoms)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeRandom_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<PerfumeTag>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeTag_pkey");

			entity.ToTable("PerfumeTag");

			entity.HasOne(d => d.Perfume).WithMany(p => p.PerfumeTags)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeTag_perfumeId_fkey");

			entity.HasOne(d => d.Tag).WithMany(p => p.PerfumeTags)
				.HasForeignKey(d => d.TagId)
				.OnDelete(DeleteBehavior.Restrict)
				.HasConstraintName("PerfumeTag_tagId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<PerfumeEvent>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeEvent_pkey");

			entity.ToTable("PerfumeEvent");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeEvents)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeEvent_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<Recommendation>(entity => {
			entity.HasKey(e => e.Id).HasName("Recommendation_pkey");

			entity.ToTable("Recommendation");

			entity.Property(e => e.CreatedAt)
				.HasDefaultValueSql("CURRENT_TIMESTAMP");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<Tag>(entity => {
			entity.HasKey(e => e.Id).HasName("Tag_pkey");

			entity.ToTable("Tag");

			entity.HasIndex(e => e.TagName, "Tag_tag_key").IsUnique();
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<Mission>(entity => {
			entity.HasKey(e => e.Id).HasName("Mission_pkey");
			entity.ToTable("Mission");
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		modelBuilder.Entity<UserMission>(entity => {
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
			entity.HasQueryFilter(x => !x.IsDeleted && x.UserId == currentUserID);
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
