using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnotherOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Roles",
                table: "Contributors");

            migrationBuilder.CreateTable(
                name: "ContributorRoles",
                columns: table => new
                {
                    ContributorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributorRoles", x => new { x.ContributorId, x.Role });
                    table.ForeignKey(
                        name: "FK_ContributorRoles_Contributors_ContributorId",
                        column: x => x.ContributorId,
                        principalTable: "Contributors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContributorRoles");

            migrationBuilder.AddColumn<int[]>(
                name: "Roles",
                table: "Contributors",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }
    }
}
