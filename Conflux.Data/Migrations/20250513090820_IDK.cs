using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class IDK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganisationRoles_Organisations_OrganisationId",
                table: "OrganisationRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Organisations_Projects_ProjectId",
                table: "Organisations");

            migrationBuilder.DropIndex(
                name: "IX_Organisations_ProjectId",
                table: "Organisations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Organisations");

            migrationBuilder.AlterColumn<string>(
                name: "RORId",
                table: "Organisations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganisationId",
                table: "OrganisationRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "OrganisationRoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles",
                columns: new[] { "ProjectId", "OrganisationId", "Role" });

            migrationBuilder.CreateTable(
                name: "ProjectOrganisations",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOrganisations", x => new { x.ProjectId, x.OrganisationId });
                    table.ForeignKey(
                        name: "FK_ProjectOrganisations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationRoles_ProjectOrganisations_ProjectId_Organisati~",
                table: "OrganisationRoles",
                columns: new[] { "ProjectId", "OrganisationId" },
                principalTable: "ProjectOrganisations",
                principalColumns: new[] { "ProjectId", "OrganisationId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganisationRoles_ProjectOrganisations_ProjectId_Organisati~",
                table: "OrganisationRoles");

            migrationBuilder.DropTable(
                name: "ProjectOrganisations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "OrganisationRoles");

            migrationBuilder.AlterColumn<string>(
                name: "RORId",
                table: "Organisations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Organisations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "OrganisationId",
                table: "OrganisationRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles",
                columns: new[] { "OrganisationId", "Role" });

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_ProjectId",
                table: "Organisations",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationRoles_Organisations_OrganisationId",
                table: "OrganisationRoles",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Organisations_Projects_ProjectId",
                table: "Organisations",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }
    }
}
