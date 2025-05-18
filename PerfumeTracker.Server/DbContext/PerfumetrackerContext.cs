namespace PerfumeTracker.Server.Models;

public partial class PerfumeTrackerContext : DbContext {
	//private int userId = 1; //TODO multi-user when needed
	public PerfumeTrackerContext() {
	}

	public PerfumeTrackerContext(DbContextOptions<PerfumeTrackerContext> options)
		: base(options) {
	}

	public virtual DbSet<Perfume> Perfumes { get; set; }
	public virtual DbSet<PerfumeRandoms> PerfumeRandoms { get; set; }
	public virtual DbSet<PerfumeTag> PerfumeTags { get; set; }
	public virtual DbSet<PerfumeWorn> PerfumeEvents { get; set; }
	public virtual DbSet<Recommendation> Recommendations { get; set; }
	public virtual DbSet<Tag> Tags { get; set; }
	public virtual DbSet<PerfumePlayList> PerfumePlayLists { get; set; }
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
			entity.HasQueryFilter(x => !x.IsDeleted);
		});
		modelBuilder.Entity<Achievement>(entity => {
			entity.HasKey(e => e.Id).HasName("Achievement_pkey");
			entity.ToTable("Achievement");
			entity.HasQueryFilter(x => !x.IsDeleted);
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

			entity.HasQueryFilter(x => !x.IsDeleted);
			//entity.HasQueryFilter(x => x.UserId == userId); //TODO multi-user when needed
		});

		modelBuilder.Entity<UserProfile>(entity => {
			entity.HasQueryFilter(x => !x.IsDeleted);
			//entity.HasQueryFilter(x => x.UserId == userId); //TODO multi-user when needed
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
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<PerfumeRandoms>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeRandom_pkey");

			entity.ToTable("PerfumeRandom");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeRandoms)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeRandom_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted);
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
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<PerfumeWorn>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeEvent_pkey");

			entity.ToTable("PerfumeEvent");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeEvents)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeEvent_perfumeId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<Recommendation>(entity => {
			entity.HasKey(e => e.Id).HasName("Recommendation_pkey");

			entity.ToTable("Recommendation");

			entity.Property(e => e.CreatedAt)
				.HasDefaultValueSql("CURRENT_TIMESTAMP");
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<Tag>(entity => {
			entity.HasKey(e => e.Id).HasName("Tag_pkey");

			entity.ToTable("Tag");

			entity.HasIndex(e => e.TagName, "Tag_tag_key").IsUnique();
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<PerfumePlayList>(entity => {
			entity.HasKey(e => e.Name).HasName("PerfumePlayList_pkey");

			entity.ToTable("PerfumePlayList");

			entity.HasMany(d => d.Perfumes)
				.WithMany(p => p.PerfumePlayList);
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<Mission>(entity => {
			entity.HasKey(e => e.Id).HasName("Mission_pkey");
			entity.ToTable("Mission");
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		modelBuilder.Entity<UserMission>(entity => {
			entity.HasKey(e => e.Id).HasName("UserMission_pkey");
			entity.ToTable("UserMission");
			
			entity.HasOne(e => e.User)
				.WithMany()
				.HasForeignKey(e => e.UserId)
				.HasConstraintName("UserMission_UserId_fkey");
				
			entity.HasOne(e => e.Mission)
				.WithMany()
				.HasForeignKey(e => e.MissionId)
				.HasConstraintName("UserMission_MissionId_fkey");
			entity.HasQueryFilter(x => !x.IsDeleted);
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
