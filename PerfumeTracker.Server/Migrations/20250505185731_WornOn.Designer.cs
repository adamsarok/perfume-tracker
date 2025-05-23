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
    [DbContext(typeof(PerfumeTrackerContext))]
    [Migration("20250505185731_WornOn")]
    partial class WornOn
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PerfumePerfumePlayList", b =>
                {
                    b.Property<string>("PerfumePlayListName")
                        .HasColumnType("text");

                    b.Property<int>("PerfumesId")
                        .HasColumnType("integer");

                    b.HasKey("PerfumePlayListName", "PerfumesId");

                    b.HasIndex("PerfumesId");

                    b.ToTable("PerfumePerfumePlayList");
                });

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

                    b.Property<DateTime>("CreatedAt")
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

                    b.Property<decimal>("Ml")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("numeric")
                        .HasDefaultValue(2m);

                    b.Property<decimal>("MlLeft")
                        .HasColumnType("numeric");

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

                    b.Property<DateTime>("UpdatedAt")
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

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumePlayList", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Name")
                        .HasName("PerfumePlayList_pkey");

                    b.ToTable("PerfumePlayList", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.PerfumeSuggested", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerfumeId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

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

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerfumeId")
                        .HasColumnType("integer");

                    b.Property<int>("TagId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

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

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("PerfumeId")
                        .HasColumnType("integer");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("WornOn")
                        .HasColumnType("timestamp with time zone");

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

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Query")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Recommendations")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id")
                        .HasName("Recommendation_pkey");

                    b.ToTable("Recommendation", (string)null);
                });

            modelBuilder.Entity("PerfumeTracker.Server.Models.Settings", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("DayFilter")
                        .HasColumnType("integer");

                    b.Property<double>("MinimumRating")
                        .HasColumnType("double precision");

                    b.Property<bool>("ShowFemalePerfumes")
                        .HasColumnType("boolean");

                    b.Property<bool>("ShowMalePerfumes")
                        .HasColumnType("boolean");

                    b.Property<bool>("ShowUnisexPerfumes")
                        .HasColumnType("boolean");

                    b.Property<decimal>("SprayAmountFullSizeMl")
                        .HasColumnType("numeric");

                    b.Property<decimal>("SprayAmountSamplesMl")
                        .HasColumnType("numeric");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("UserId")
                        .HasName("Settings_pkey");

                    b.ToTable("Settings", (string)null);
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

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("TagName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id")
                        .HasName("Tag_pkey");

                    b.HasIndex(new[] { "TagName" }, "Tag_tag_key")
                        .IsUnique();

                    b.ToTable("Tag", (string)null);
                });

            modelBuilder.Entity("PerfumePerfumePlayList", b =>
                {
                    b.HasOne("PerfumeTracker.Server.Models.PerfumePlayList", null)
                        .WithMany()
                        .HasForeignKey("PerfumePlayListName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PerfumeTracker.Server.Models.Perfume", null)
                        .WithMany()
                        .HasForeignKey("PerfumesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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
