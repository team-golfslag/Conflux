// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class SystemAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "AssignedLectorates",
                table: "Users",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "AssignedOrganisations",
                table: "Users",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "PermissionLevel",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Lectorate",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerOrganisation",
                table: "Projects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedLectorates",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedOrganisations",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PermissionLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Lectorate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OwnerOrganisation",
                table: "Projects");
        }
    }
}
