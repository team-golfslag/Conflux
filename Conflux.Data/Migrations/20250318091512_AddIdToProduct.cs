using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductProject_Products_ProductsUrl",
                table: "ProductProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductProject",
                table: "ProductProject");

            migrationBuilder.DropColumn(
                name: "ProductsUrl",
                table: "ProductProject");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProductsId",
                table: "ProductProject",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductProject",
                table: "ProductProject",
                columns: new[] { "ProductsId", "ProjectId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductProject_Products_ProductsId",
                table: "ProductProject",
                column: "ProductsId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductProject_Products_ProductsId",
                table: "ProductProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductProject",
                table: "ProductProject");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductsId",
                table: "ProductProject");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductsUrl",
                table: "ProductProject",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Url");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductProject",
                table: "ProductProject",
                columns: new[] { "ProductsUrl", "ProjectId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProductProject_Products_ProductsUrl",
                table: "ProductProject",
                column: "ProductsUrl",
                principalTable: "Products",
                principalColumn: "Url",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
