namespace PerfumeTracker.Server.Models;

public partial class PerfumetrackerContext : DbContext {
	public PerfumetrackerContext() {
	}

	public PerfumetrackerContext(DbContextOptions<PerfumetrackerContext> options)
		: base(options) {
	}

	public virtual DbSet<Perfume> Perfumes { get; set; }
	public virtual DbSet<PerfumeSuggested> PerfumeSuggesteds { get; set; }
	public virtual DbSet<PerfumeTag> PerfumeTags { get; set; }
	public virtual DbSet<PerfumeWorn> PerfumeWorns { get; set; }
	public virtual DbSet<Recommendation> Recommendations { get; set; }
	public virtual DbSet<Tag> Tags { get; set; }
	public virtual DbSet<PerfumePlayList> PerfumePlayLists { get; set; }
	public virtual DbSet<Settings> Settings { get; set; }
	protected override void OnModelCreating(ModelBuilder modelBuilder) {
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
		});

		modelBuilder.Entity<PerfumeSuggested>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeSuggested_pkey");

			entity.ToTable("PerfumeSuggested");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeSuggesteds)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeSuggested_perfumeId_fkey");
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
		});

		modelBuilder.Entity<PerfumeWorn>(entity => {
			entity.HasKey(e => e.Id).HasName("PerfumeWorn_pkey");

			entity.ToTable("PerfumeWorn");

			entity.HasOne(d => d.Perfume)
				.WithMany(p => p.PerfumeWorns)
				.HasForeignKey(d => d.PerfumeId)
				.HasConstraintName("PerfumeWorn_perfumeId_fkey");
		});

		modelBuilder.Entity<Recommendation>(entity => {
			entity.HasKey(e => e.Id).HasName("Recommendation_pkey");

			entity.ToTable("Recommendation");

			entity.Property(e => e.Created_At)
				.HasDefaultValueSql("CURRENT_TIMESTAMP");
		});

		modelBuilder.Entity<Tag>(entity => {
			entity.HasKey(e => e.Id).HasName("Tag_pkey");

			entity.ToTable("Tag");

			entity.HasIndex(e => e.TagName, "Tag_tag_key").IsUnique();
		});

		modelBuilder.Entity<PerfumePlayList>(entity => {
			entity.HasKey(e => e.Name).HasName("PerfumePlayList_pkey");

			entity.ToTable("PerfumePlayList");

			entity.HasMany(d => d.Perfumes)
				.WithMany(p => p.PerfumePlayList);
		});

		modelBuilder.Entity<Settings>(entity => {
			entity.HasKey(e => e.UserId).HasName("Settings_pkey");

			entity.ToTable("Settings");
		});

		OnModelCreatingPartial(modelBuilder);
	}

	partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
