using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserRoleEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "UserRoles");

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "UserRoles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "UserRoles");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "UserRoles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "UserRoles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
