using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRAiDInfoToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RAiDId",
                table: "Projects",
                newName: "RAiDInfo_RAiDId");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastestEdit",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastestEdit",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_Dirty",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_LatestSync",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "RAiDInfo_RAiDId",
                table: "Projects",
                newName: "RAiDId");
        }
    }
}
