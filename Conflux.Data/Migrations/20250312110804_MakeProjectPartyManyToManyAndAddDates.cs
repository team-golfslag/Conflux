using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProjectPartyManyToManyAndAddDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Parties_PartyId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_PartyId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "PartyId",
                table: "Projects");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Parties",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ProjectId",
                table: "Parties",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Parties_Projects_ProjectId",
                table: "Parties",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Parties_Projects_ProjectId",
                table: "Parties");

            migrationBuilder.DropIndex(
                name: "IX_Parties_ProjectId",
                table: "Parties");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Parties");

            migrationBuilder.AddColumn<Guid>(
                name: "PartyId",
                table: "Projects",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Projects_PartyId",
                table: "Projects",
                column: "PartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Parties_PartyId",
                table: "Projects",
                column: "PartyId",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
