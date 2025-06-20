// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSemanticSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "Projects",
                type: "vector(384)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingContentHash",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingLastUpdated",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTitles_Text",
                table: "ProjectTitles",
                column: "Text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Embedding",
                table: "Projects",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" })
                .Annotation("Npgsql:StorageParameter:ef_construction", 64)
                .Annotation("Npgsql:StorageParameter:m", 16);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SCIMId",
                table: "Projects",
                column: "SCIMId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDescriptions_Text",
                table: "ProjectDescriptions",
                column: "Text")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTitles_Text",
                table: "ProjectTitles");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Embedding",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_SCIMId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDescriptions_Text",
                table: "ProjectDescriptions");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EmbeddingContentHash",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EmbeddingLastUpdated",
                table: "Projects");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
