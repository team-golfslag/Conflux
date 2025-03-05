﻿// <auto-generated />
using System;
using Conflux.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Conflux.Data.Migrations
{
    [DbContext(typeof(ConfluxContext))]
    partial class ConfluxContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Conflux.Domain.Party", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Parties");
                });

            modelBuilder.Entity("Conflux.Domain.Person", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("Age")
                        .HasColumnType("integer");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.ToTable("People");
                });

            modelBuilder.Entity("Conflux.Domain.Product", b =>
                {
                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Url");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("Conflux.Domain.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<Guid>("PartyId")
                        .HasColumnType("uuid");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("PartyId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("PersonProject", b =>
                {
                    b.Property<Guid>("PeopleId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.HasKey("PeopleId", "ProjectId");

                    b.HasIndex("ProjectId");

                    b.ToTable("PersonProject");
                });

            modelBuilder.Entity("ProductProject", b =>
                {
                    b.Property<string>("ProductsUrl")
                        .HasColumnType("text");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.HasKey("ProductsUrl", "ProjectId");

                    b.HasIndex("ProjectId");

                    b.ToTable("ProductProject");
                });

            modelBuilder.Entity("Conflux.Domain.Project", b =>
                {
                    b.HasOne("Conflux.Domain.Party", "Party")
                        .WithMany()
                        .HasForeignKey("PartyId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Party");
                });

            modelBuilder.Entity("PersonProject", b =>
                {
                    b.HasOne("Conflux.Domain.Person", null)
                        .WithMany()
                        .HasForeignKey("PeopleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Conflux.Domain.Project", null)
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ProductProject", b =>
                {
                    b.HasOne("Conflux.Domain.Product", null)
                        .WithMany()
                        .HasForeignKey("ProductsUrl")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Conflux.Domain.Project", null)
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
