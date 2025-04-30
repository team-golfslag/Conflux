using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class IDontKnow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributorPositions_Contributors_ContributorId",
                table: "ContributorPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributorRoles_Contributors_ContributorId",
                table: "ContributorRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Contributors_Projects_ProjectId",
                table: "Contributors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contributors",
                table: "Contributors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributorRoles",
                table: "ContributorRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributorPositions",
                table: "ContributorPositions");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Contributors");

            migrationBuilder.DropColumn(
                name: "FamilyName",
                table: "Contributors");

            migrationBuilder.DropColumn(
                name: "GivenName",
                table: "Contributors");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Contributors");

            migrationBuilder.DropColumn(
                name: "ORCiD",
                table: "Contributors");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Contributors",
                newName: "PersonId");

            migrationBuilder.RenameColumn(
                name: "ContributorId",
                table: "ContributorRoles",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "ContributorId",
                table: "ContributorPositions",
                newName: "ProjectId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "Contributors",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Contact",
                table: "Contributors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Leader",
                table: "Contributors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"))
                .Annotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contributors",
                table: "Contributors",
                columns: new[] { "PersonId", "ProjectId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributorRoles",
                table: "ContributorRoles",
                columns: new[] { "PersonId", "ProjectId", "RoleType" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributorPositions",
                table: "ContributorPositions",
                columns: new[] { "PersonId", "ProjectId", "Position" });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ORCiD = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GivenName = table.Column<string>(type: "text", nullable: true),
                    FamilyName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_ContributorPositions_Contributors_PersonId_ProjectId",
                table: "ContributorPositions",
                columns: new[] { "PersonId", "ProjectId" },
                principalTable: "Contributors",
                principalColumns: new[] { "PersonId", "ProjectId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributorRoles_Contributors_PersonId_ProjectId",
                table: "ContributorRoles",
                columns: new[] { "PersonId", "ProjectId" },
                principalTable: "Contributors",
                principalColumns: new[] { "PersonId", "ProjectId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contributors_Projects_ProjectId",
                table: "Contributors",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContributorPositions_Contributors_PersonId_ProjectId",
                table: "ContributorPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_ContributorRoles_Contributors_PersonId_ProjectId",
                table: "ContributorRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Contributors_Projects_ProjectId",
                table: "Contributors");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Contributors",
                table: "Contributors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributorRoles",
                table: "ContributorRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ContributorPositions",
                table: "ContributorPositions");

            migrationBuilder.DropColumn(
                name: "Contact",
                table: "Contributors");

            migrationBuilder.DropColumn(
                name: "Leader",
                table: "Contributors");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "ContributorRoles");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "ContributorPositions");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "Contributors",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "ContributorRoles",
                newName: "ContributorId");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "ContributorPositions",
                newName: "ContributorId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "Contributors",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Contributors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FamilyName",
                table: "Contributors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GivenName",
                table: "Contributors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Contributors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORCiD",
                table: "Contributors",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContributorId",
                table: "ContributorRoles",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<Guid>(
                name: "ContributorId",
                table: "ContributorPositions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contributors",
                table: "Contributors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributorRoles",
                table: "ContributorRoles",
                columns: new[] { "ContributorId", "RoleType" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ContributorPositions",
                table: "ContributorPositions",
                columns: new[] { "ContributorId", "Position" });

            migrationBuilder.AddForeignKey(
                name: "FK_ContributorPositions_Contributors_ContributorId",
                table: "ContributorPositions",
                column: "ContributorId",
                principalTable: "Contributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContributorRoles_Contributors_ContributorId",
                table: "ContributorRoles",
                column: "ContributorId",
                principalTable: "Contributors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Contributors_Projects_ProjectId",
                table: "Contributors",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }
    }
}
