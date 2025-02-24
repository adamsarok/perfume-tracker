using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PerfumeTrackerAPI.Models;

public partial class PerfumetrackerContext : DbContext
{
    public PerfumetrackerContext()
    {
    }

    public PerfumetrackerContext(DbContextOptions<PerfumetrackerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Perfume> Perfumes { get; set; }

    public virtual DbSet<PerfumeSuggested> PerfumeSuggesteds { get; set; }

    public virtual DbSet<PerfumeTag> PerfumeTags { get; set; }

    public virtual DbSet<PerfumeWorn> PerfumeWorns { get; set; }

    public virtual DbSet<Recommendation> Recommendations { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //modelBuilder.HasPostgresExtension("pgagent", "pgagent");

        modelBuilder.Entity<Perfume>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Perfume_pkey");

            entity.ToTable("Perfume");

            //entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Autumn)
                .HasDefaultValue(true);
                //.HasColumnName("autumn");
            //entity.Property(e => e.House).HasColumnName("house");
            entity.Property(e => e.ImageObjectKey)
                .HasDefaultValueSql("''::text");
                //.HasColumnName("imageObjectKey");
            entity.Property(e => e.Ml)
                .HasDefaultValue(2);
                //.HasColumnName("ml");
            entity.Property(e => e.Notes)
                .HasDefaultValueSql("''::text");
                //.HasColumnName("notes");
            // entity.Property(e => e.PerfumeName).HasColumnName("perfume");
            // entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Spring)
                .HasDefaultValue(true);
                //.HasColumnName("spring");
            entity.Property(e => e.Summer)
                .HasDefaultValue(true);
                //.HasColumnName("summer");
            entity.Property(e => e.Winter)
                .HasDefaultValue(true);
                //.HasColumnName("winter");
            entity.HasGeneratedTsVectorColumn(
                p => p.FullText,
                "english", 
                p => new { p.PerfumeName, p.House, p.Notes, p.Rating })
                .HasIndex(p => p.FullText)
                .HasMethod("GIN");
            entity
                .HasIndex(p => new { p.House, p.PerfumeName }).IsUnique();
        });

        modelBuilder.Entity<PerfumeSuggested>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PerfumeSuggested_pkey");

            entity.ToTable("PerfumeSuggested");

            // entity.Property(e => e.Id).HasColumnName("id");
            // entity.Property(e => e.PerfumeId).HasColumnName("perfumeId");
            // entity.Property(e => e.Created_At);

            entity.HasOne(d => d.Perfume)
                .WithMany(p => p.PerfumeSuggesteds)
                .HasForeignKey(d => d.PerfumeId)
                .HasConstraintName("PerfumeSuggested_perfumeId_fkey");
        });

        modelBuilder.Entity<PerfumeTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PerfumeTag_pkey");

            entity.ToTable("PerfumeTag");

            // entity.Property(e => e.Id).HasColumnName("id");
            // entity.Property(e => e.PerfumeId).HasColumnName("perfumeId");
            // entity.Property(e => e.TagId).HasColumnName("tagId");

            entity.HasOne(d => d.Perfume).WithMany(p => p.PerfumeTags)
                .HasForeignKey(d => d.PerfumeId)
                .HasConstraintName("PerfumeTag_perfumeId_fkey");

            entity.HasOne(d => d.Tag).WithMany(p => p.PerfumeTags)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("PerfumeTag_tagId_fkey");
        });

        modelBuilder.Entity<PerfumeWorn>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PerfumeWorn_pkey");

            entity.ToTable("PerfumeWorn");

            // entity.Property(e => e.Id).HasColumnName("id");
            // entity.Property(e => e.PerfumeId).HasColumnName("perfumeId");
            // entity.Property(e => e.WornOn)
            //     .HasColumnType("timestamp(3) without time zone")
            //     .HasColumnName("wornOn");

            entity.HasOne(d => d.Perfume)
                .WithMany(p => p.PerfumeWorns)
                .HasForeignKey(d => d.PerfumeId)
                .HasConstraintName("PerfumeWorn_perfumeId_fkey");
        });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Recommendation_pkey");

            entity.ToTable("Recommendation");

            //entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Created_At)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            // entity.Property(e => e.Query).HasColumnName("query");
            // entity.Property(e => e.Recommendations).HasColumnName("recommendations");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tag_pkey");

            entity.ToTable("Tag");

            entity.HasIndex(e => e.TagName, "Tag_tag_key").IsUnique();

            // entity.Property(e => e.Id).HasColumnName("id");
            // entity.Property(e => e.Color).HasColumnName("color");
            // entity.Property(e => e.TagName).HasColumnName("tag");
        });



        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
