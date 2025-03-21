﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;
using PerfumeTracker.Server.Models;

#nullable disable

namespace PerfumeTracker.Server.Migrations
{
    [DbContext(typeof(PerfumetrackerContext))]
    [Migration("20241209145839_init")]
    partial class init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            //NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "pgagent", "pgagent");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PerfumeTracker.Server.Models.Perfume", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<bool>("Autumn")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTime>("Created_At")
                        .HasColumnType("timestamp with time zone");

                    b.Property<NpgsqlTsVector>("FullText")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("tsvector")
                        .HasAnnotation("Npgsql:TsVectorConfig", "english")
                        .HasAnnotation("Npgsql:TsVectorProperties", new[] { "PerfumeName", "House", "Notes", "Rating" });

                    b.Property<string>("House")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ImageObjectKey")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValueSql("''::text");

                    b.Property<int>("Ml")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(2);

                    b.Property<string>("Notes")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasColumnType("text")
                        .HasDefaultValueSql("''::text");

                    b.Property<string>("PerfumeName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<double>("Rating")
                        .HasColumnType("double precision");

                    b.Property<bool>("Spring")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<bool>("Summer")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.Property<DateTime?>("Updated_At")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("Winter")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true);

                    b.HasKey("Id")
                        .HasName("Perfume_pkey");

                    b.HasIndex("FullText");

                    NpgsqlIndexBuilderExtensions.HasMethod(b.HasIndex("FullText"), "GIN");

                    b.HasIndex("House", "PerfumeName")
                        .IsUnique();

                    b.ToTable("Perfume", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeSuggested", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Created_At")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerfumeId")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("PerfumeSuggested_pkey");

                    b.HasIndex("PerfumeId");

                    b.ToTable("PerfumeSuggested", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Created_At")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerfumeId")
                        .HasColumnType("integer");

                    b.Property<int>("TagId")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("PerfumeTag_pkey");

                    b.HasIndex("PerfumeId");

                    b.HasIndex("TagId");

                    b.ToTable("PerfumeTag", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeWorn", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Created_At")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerfumeId")
                        .HasColumnType("integer");

                    b.HasKey("Id")
                        .HasName("PerfumeWorn_pkey");

                    b.HasIndex("PerfumeId");

                    b.ToTable("PerfumeWorn", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.Recommendation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("Created_At")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Query")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Recommendations")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("Recommendation_pkey");

                    b.ToTable("Recommendation", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Color")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Created_At")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TagName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("Updated_At")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id")
                        .HasName("Tag_pkey");

                    b.HasIndex(new[] { "TagName" }, "Tag_tag_key")
                        .IsUnique();

                    b.ToTable("Tag", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeSuggested", b =>
                {
                    b.HasOne("PerfumeTracker.Server.Models.Perfume", "Perfume")
                        .WithMany("PerfumeSuggesteds")
                        .HasForeignKey("PerfumeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("PerfumeSuggested_perfumeId_fkey");

                    b.Navigation("Perfume");
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeTag", b =>
                {
                    b.HasOne("PerfumeTracker.Server.Models.Perfume", "Perfume")
                        .WithMany("PerfumeTags")
                        .HasForeignKey("PerfumeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("PerfumeTag_perfumeId_fkey");

                    b.HasOne("PerfumeTracker.Server.Models.Tag", "Tag")
                        .WithMany("PerfumeTags")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("PerfumeTag_tagId_fkey");

                    b.Navigation("Perfume");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeWorn", b =>
                {
                    b.HasOne("PerfumeTracker.Server.Models.Perfume", "Perfume")
                        .WithMany("PerfumeWorns")
                        .HasForeignKey("PerfumeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("PerfumeWorn_perfumeId_fkey");

                    b.Navigation("Perfume");
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.Perfume", b =>
                {
                    b.Navigation("PerfumeSuggesteds");

                    b.Navigation("PerfumeTags");

                    b.Navigation("PerfumeWorns");
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.Tag", b =>
                {
                    b.Navigation("PerfumeTags");
                });
#pragma warning restore 612, 618
        }
    }
}
