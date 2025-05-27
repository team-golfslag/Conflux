using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreRAiDInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RAiDInfo_Dirty",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_LatestSync",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_RAiDId",
                table: "Projects");

            migrationBuilder.CreateTable(
                name: "RAiDInfos",
                columns: table => new
                {
                    projectId = table.Column<Guid>(type: "uuid", nullable: false),
                    LatestSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Dirty = table.Column<bool>(type: "boolean", nullable: false),
                    RAiDId = table.Column<string>(type: "text", nullable: false),
                    RegistrationAgencyId = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<string>(type: "text", nullable: false),
                    OwnerServicePoint = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RAiDInfos", x => x.projectId);
                    table.ForeignKey(
                        name: "FK_RAiDInfos_Projects_projectId",
                        column: x => x.projectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RAiDInfos");

            migrationBuilder.AddColumn<bool>(
                name: "RAiDInfo_Dirty",
                table: "Projects",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RAiDInfo_LatestSync",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RAiDInfo_RAiDId",
                table: "Projects",
                type: "text",
                nullable: true);
        }
    }
}
