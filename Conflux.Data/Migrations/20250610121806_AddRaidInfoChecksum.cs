using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidInfoChecksum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Checksum",
                table: "RAiDInfos",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Checksum",
                table: "RAiDInfos");
        }
    }
}
