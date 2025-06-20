// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimaryKeyForOrganisationRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "OrganisationRoles",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.CreateVersion7());

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationRoles_ProjectId_OrganisationId",
                table: "OrganisationRoles",
                columns: new[] { "ProjectId", "OrganisationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles");

            migrationBuilder.DropIndex(
                name: "IX_OrganisationRoles_ProjectId_OrganisationId",
                table: "OrganisationRoles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "OrganisationRoles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles",
                columns: new[] { "ProjectId", "OrganisationId", "Role" });
        }
    }
}
