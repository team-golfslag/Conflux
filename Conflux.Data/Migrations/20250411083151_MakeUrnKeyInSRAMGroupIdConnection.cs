using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeUrnKeyInSRAMGroupIdConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SRAMGroupIdConnections",
                table: "SRAMGroupIdConnections");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "SRAMGroupIdConnections",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SRAMGroupIdConnections",
                table: "SRAMGroupIdConnections",
                column: "Urn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SRAMGroupIdConnections",
                table: "SRAMGroupIdConnections");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "SRAMGroupIdConnections",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SRAMGroupIdConnections",
                table: "SRAMGroupIdConnections",
                column: "Id");
        }
    }
}
