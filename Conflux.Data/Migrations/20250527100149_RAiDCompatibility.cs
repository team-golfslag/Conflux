using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class RAiDCompatibility : Migration
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

            migrationBuilder.DropTable(
                name: "ProductProject");

            migrationBuilder.DropTable(
                name: "ProjectProjectDescription");

            migrationBuilder.DropTable(
                name: "ProjectProjectTitle");

            migrationBuilder.DropIndex(
                name: "IX_Organisations_ProjectId",
                table: "Organisations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_Dirty",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_LatestSync",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RAiDInfo_RAiDId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Organisations");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Schema",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "RORId",
                table: "Organisations",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "OrganisationRoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "PersonId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "PersonId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 0);

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
                        name: "FK_ProjectOrganisations_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectOrganisations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RAiDInfos",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_RAiDInfos", x => x.ProjectId);
                    table.ForeignKey(
                        name: "FK_RAiDInfos_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTitles_ProjectId",
                table: "ProjectTitles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDescriptions_ProjectId",
                table: "ProjectDescriptions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProjectId",
                table: "Products",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOrganisations_OrganisationId",
                table: "ProjectOrganisations",
                column: "OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contributors_People_PersonId",
                table: "Contributors",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationRoles_ProjectOrganisations_ProjectId_Organisati~",
                table: "OrganisationRoles",
                columns: new[] { "ProjectId", "OrganisationId" },
                principalTable: "ProjectOrganisations",
                principalColumns: new[] { "ProjectId", "OrganisationId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Projects_ProjectId",
                table: "Products",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDescriptions_Projects_ProjectId",
                table: "ProjectDescriptions",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTitles_Projects_ProjectId",
                table: "ProjectTitles",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contributors_People_PersonId",
                table: "Contributors");

            migrationBuilder.DropForeignKey(
                name: "FK_OrganisationRoles_ProjectOrganisations_ProjectId_Organisati~",
                table: "OrganisationRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Projects_ProjectId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDescriptions_Projects_ProjectId",
                table: "ProjectDescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTitles_Projects_ProjectId",
                table: "ProjectTitles");

            migrationBuilder.DropTable(
                name: "ProjectOrganisations");

            migrationBuilder.DropTable(
                name: "RAiDInfos");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTitles_ProjectId",
                table: "ProjectTitles");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDescriptions_ProjectId",
                table: "ProjectDescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProjectId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "OrganisationRoles");

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

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Schema",
                table: "Products",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

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
                name: "ProjectId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "PersonId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "PersonId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrganisationRoles",
                table: "OrganisationRoles",
                columns: new[] { "OrganisationId", "Role" });

            migrationBuilder.CreateTable(
                name: "ProductProject",
                columns: table => new
                {
                    ProductsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductProject", x => new { x.ProductsId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_ProductProject_Products_ProductsId",
                        column: x => x.ProductsId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductProject_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProjectDescription",
                columns: table => new
                {
                    DescriptionsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProjectDescription", x => new { x.DescriptionsId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_ProjectProjectDescription_ProjectDescriptions_DescriptionsId",
                        column: x => x.DescriptionsId,
                        principalTable: "ProjectDescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectProjectDescription_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_ProjectProjectTitle_ProjectTitles_TitlesId",
                        column: x => x.TitlesId,
                        principalTable: "ProjectTitles",
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
                name: "IX_Organisations_ProjectId",
                table: "Organisations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductProject_ProjectId",
                table: "ProductProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProjectDescription_ProjectId",
                table: "ProjectProjectDescription",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProjectTitle_TitlesId",
                table: "ProjectProjectTitle",
                column: "TitlesId");

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
