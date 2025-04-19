using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSRAMAndRAiDIdAlsoAddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RAiDId",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SRAMId",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "People",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyName",
                table: "People",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GivenName",
                table: "People",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ORCiD",
                table: "People",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SRAMId",
                table: "People",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Urn = table.Column<string>(type: "text", nullable: false),
                    PersonId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_PersonId",
                table: "Roles",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropColumn(
                name: "RAiDId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "SRAMId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "People");

            migrationBuilder.DropColumn(
                name: "FamilyName",
                table: "People");

            migrationBuilder.DropColumn(
                name: "GivenName",
                table: "People");

            migrationBuilder.DropColumn(
                name: "ORCiD",
                table: "People");

            migrationBuilder.DropColumn(
                name: "SRAMId",
                table: "People");
        }
    }
}
