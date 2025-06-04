using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAdminProps : Migration
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
                name: "Tier",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
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
                name: "Tier",
                table: "Users");
        }
    }
}
