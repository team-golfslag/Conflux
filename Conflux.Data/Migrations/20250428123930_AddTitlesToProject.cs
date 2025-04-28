using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTitlesToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectProjectTitle_ProjectTitle_TitlesId",
                table: "ProjectProjectTitle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectTitle",
                table: "ProjectTitle");

            migrationBuilder.RenameTable(
                name: "ProjectTitle",
                newName: "ProjectTitles");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectTitles",
                table: "ProjectTitles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectProjectTitle_ProjectTitles_TitlesId",
                table: "ProjectProjectTitle",
                column: "TitlesId",
                principalTable: "ProjectTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectProjectTitle_ProjectTitles_TitlesId",
                table: "ProjectProjectTitle");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectTitles",
                table: "ProjectTitles");

            migrationBuilder.RenameTable(
                name: "ProjectTitles",
                newName: "ProjectTitle");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectTitle",
                table: "ProjectTitle",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectProjectTitle_ProjectTitle_TitlesId",
                table: "ProjectProjectTitle",
                column: "TitlesId",
                principalTable: "ProjectTitle",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
