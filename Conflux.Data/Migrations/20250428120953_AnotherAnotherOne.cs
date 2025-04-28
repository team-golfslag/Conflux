using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AnotherAnotherOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Projects");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectTitle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Language = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTitle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProjectTitle",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TitlesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProjectTitle", x => new { x.ProjectId, x.TitlesId });
                    table.ForeignKey(
                        name: "FK_ProjectProjectTitle_ProjectTitle_TitlesId",
                        column: x => x.TitlesId,
                        principalTable: "ProjectTitle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectProjectTitle_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProjectTitle_TitlesId",
                table: "ProjectProjectTitle",
                column: "TitlesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectProjectTitle");

            migrationBuilder.DropTable(
                name: "ProjectTitle");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
