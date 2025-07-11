﻿// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System;
using System.Collections.Generic;
using Conflux.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Conflux.Data.Migrations
{
    [DbContext(typeof(ConfluxContext))]
    [Migration("20250610121806_AddRaidInfoChecksum")]
    partial class AddRaidInfoChecksum
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Conflux.Domain.Contributor", b =>
                {
                    b.Property<Guid>("PersonId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<bool>("Contact")
                        .HasColumnType("boolean");

                    b.Property<bool>("Leader")
                        .HasColumnType("boolean");

                    b.HasKey("PersonId", "ProjectId");

                    b.HasIndex("ProjectId");

                    b.ToTable("Contributors");
                });

            modelBuilder.Entity("Conflux.Domain.ContributorPosition", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("PersonId")
                        .HasColumnType("uuid");

                    b.Property<int>("Position")
                        .HasColumnType("integer");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("PersonId", "ProjectId");

                    b.ToTable("ContributorPositions");
                });

            modelBuilder.Entity("Conflux.Domain.ContributorRole", b =>
                {
                    b.Property<Guid>("PersonId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<int>("RoleType")
                        .HasColumnType("integer");

                    b.HasKey("PersonId", "ProjectId", "RoleType");

                    b.ToTable("ContributorRoles");
                });

            modelBuilder.Entity("Conflux.Domain.Organisation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("RORId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Organisations");
                });

            modelBuilder.Entity("Conflux.Domain.OrganisationRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("OrganisationId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId", "OrganisationId");

                    b.ToTable("OrganisationRoles");
                });

            modelBuilder.Entity("Conflux.Domain.Person", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Email")
                        .HasColumnType("text");

                    b.Property<string>("FamilyName")
                        .HasColumnType("text");

                    b.Property<string>("GivenName")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ORCiD")
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "orcid_id");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.ToTable("People");
                });

            modelBuilder.Entity("Conflux.Domain.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.PrimitiveCollection<int[]>("Categories")
                        .IsRequired()
                        .HasColumnType("integer[]");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<int>("Schema")
                        .HasColumnType("integer");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<string>("Url")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("Conflux.Domain.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("LastestEdit")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Lectorate")
                        .HasColumnType("text");

                    b.Property<string>("OwnerOrganisation")
                        .HasColumnType("text");

                    b.Property<string>("SCIMId")
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "scim_id");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectDescription", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("ProjectDescriptions");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectOrganisation", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid")
                        .HasColumnOrder(0);

                    b.Property<Guid>("OrganisationId")
                        .HasColumnType("uuid")
                        .HasColumnOrder(1);

                    b.HasKey("ProjectId", "OrganisationId");

                    b.HasIndex("OrganisationId");

                    b.ToTable("ProjectOrganisations");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectTitle", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("EndDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("StartDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("ProjectTitles");
                });

            modelBuilder.Entity("Conflux.Domain.RAiDInfo", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<string>("Checksum")
                        .HasColumnType("text");

                    b.Property<bool>("Dirty")
                        .HasColumnType("boolean");

                    b.Property<DateTime?>("LatestSync")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("OwnerId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long?>("OwnerServicePoint")
                        .HasColumnType("bigint");

                    b.Property<string>("RAiDId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "raid_id");

                    b.Property<string>("RegistrationAgencyId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Version")
                        .HasColumnType("integer");

                    b.HasKey("ProjectId");

                    b.ToTable("RAiDInfos");

                    b.HasAnnotation("Relational:JsonPropertyName", "raid_info");
                });

            modelBuilder.Entity("Conflux.Domain.SRAMGroupIdConnection", b =>
                {
                    b.Property<string>("Urn")
                        .HasColumnType("text");

                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.HasKey("Urn");

                    b.ToTable("SRAMGroupIdConnections");
                });

            modelBuilder.Entity("Conflux.Domain.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.PrimitiveCollection<List<string>>("AssignedLectorates")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.PrimitiveCollection<List<string>>("AssignedOrganisations")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.PrimitiveCollection<List<Guid>>("FavoriteProjectIds")
                        .IsRequired()
                        .HasColumnType("uuid[]");

                    b.Property<int>("PermissionLevel")
                        .HasColumnType("integer");

                    b.Property<Guid>("PersonId")
                        .HasColumnType("uuid");

                    b.PrimitiveCollection<List<Guid>>("RecentlyAccessedProjectIds")
                        .IsRequired()
                        .HasColumnType("uuid[]");

                    b.Property<string>("SCIMId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "scim_id");

                    b.Property<string>("SRAMId")
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "sram_id");

                    b.HasKey("Id");

                    b.HasIndex("PersonId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Conflux.Domain.UserRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<string>("SCIMId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "scim_id");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<string>("Urn")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("UserRoles");
                });

            modelBuilder.Entity("ProjectUser", b =>
                {
                    b.Property<Guid>("ProjectId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UsersId")
                        .HasColumnType("uuid");

                    b.HasKey("ProjectId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("ProjectUser");
                });

            modelBuilder.Entity("UserUserRole", b =>
                {
                    b.Property<Guid>("RolesId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("RolesId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("UserUserRole");
                });

            modelBuilder.Entity("Conflux.Domain.Contributor", b =>
                {
                    b.HasOne("Conflux.Domain.Person", "Person")
                        .WithMany("Contributors")
                        .HasForeignKey("PersonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Conflux.Domain.Project", "Project")
                        .WithMany("Contributors")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Person");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Conflux.Domain.ContributorPosition", b =>
                {
                    b.HasOne("Conflux.Domain.Contributor", "Contributor")
                        .WithMany("Positions")
                        .HasForeignKey("PersonId", "ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Contributor");
                });

            modelBuilder.Entity("Conflux.Domain.ContributorRole", b =>
                {
                    b.HasOne("Conflux.Domain.Contributor", "Contributor")
                        .WithMany("Roles")
                        .HasForeignKey("PersonId", "ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Contributor");
                });

            modelBuilder.Entity("Conflux.Domain.OrganisationRole", b =>
                {
                    b.HasOne("Conflux.Domain.ProjectOrganisation", "Organisation")
                        .WithMany("Roles")
                        .HasForeignKey("ProjectId", "OrganisationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Organisation");
                });

            modelBuilder.Entity("Conflux.Domain.Product", b =>
                {
                    b.HasOne("Conflux.Domain.Project", "Project")
                        .WithMany("Products")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectDescription", b =>
                {
                    b.HasOne("Conflux.Domain.Project", "Project")
                        .WithMany("Descriptions")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("Conflux.Domain.Language", "Language", b1 =>
                        {
                            b1.Property<Guid>("ProjectDescriptionId")
                                .HasColumnType("uuid");

                            b1.Property<string>("Id")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)");

                            b1.HasKey("ProjectDescriptionId");

                            b1.ToTable("ProjectDescriptions");

                            b1.WithOwner()
                                .HasForeignKey("ProjectDescriptionId");
                        });

                    b.Navigation("Language");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectOrganisation", b =>
                {
                    b.HasOne("Conflux.Domain.Organisation", "Organisation")
                        .WithMany("Projects")
                        .HasForeignKey("OrganisationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Conflux.Domain.Project", "Project")
                        .WithMany("Organisations")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Organisation");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectTitle", b =>
                {
                    b.HasOne("Conflux.Domain.Project", "Project")
                        .WithMany("Titles")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("Conflux.Domain.Language", "Language", b1 =>
                        {
                            b1.Property<Guid>("ProjectTitleId")
                                .HasColumnType("uuid");

                            b1.Property<string>("Id")
                                .IsRequired()
                                .HasMaxLength(3)
                                .HasColumnType("character varying(3)");

                            b1.HasKey("ProjectTitleId");

                            b1.ToTable("ProjectTitles");

                            b1.WithOwner()
                                .HasForeignKey("ProjectTitleId");
                        });

                    b.Navigation("Language");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Conflux.Domain.RAiDInfo", b =>
                {
                    b.HasOne("Conflux.Domain.Project", "Project")
                        .WithOne("RAiDInfo")
                        .HasForeignKey("Conflux.Domain.RAiDInfo", "ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Conflux.Domain.User", b =>
                {
                    b.HasOne("Conflux.Domain.Person", "Person")
                        .WithOne("User")
                        .HasForeignKey("Conflux.Domain.User", "PersonId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Person");
                });

            modelBuilder.Entity("ProjectUser", b =>
                {
                    b.HasOne("Conflux.Domain.Project", null)
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Conflux.Domain.User", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("UserUserRole", b =>
                {
                    b.HasOne("Conflux.Domain.UserRole", null)
                        .WithMany()
                        .HasForeignKey("RolesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Conflux.Domain.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Conflux.Domain.Contributor", b =>
                {
                    b.Navigation("Positions");

                    b.Navigation("Roles");
                });

            modelBuilder.Entity("Conflux.Domain.Organisation", b =>
                {
                    b.Navigation("Projects");
                });

            modelBuilder.Entity("Conflux.Domain.Person", b =>
                {
                    b.Navigation("Contributors");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Conflux.Domain.Project", b =>
                {
                    b.Navigation("Contributors");

                    b.Navigation("Descriptions");

                    b.Navigation("Organisations");

                    b.Navigation("Products");

                    b.Navigation("RAiDInfo");

                    b.Navigation("Titles");
                });

            modelBuilder.Entity("Conflux.Domain.ProjectOrganisation", b =>
                {
                    b.Navigation("Roles");
                });
#pragma warning restore 612, 618
        }
    }
}
