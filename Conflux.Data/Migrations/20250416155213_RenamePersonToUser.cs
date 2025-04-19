using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Conflux.Data.Migrations
{
    /// <summary>
    /// Custom migration to rename Person to User in the database.
    /// </summary>
    public partial class RenamePersonToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First create the new tables
            migrationBuilder.CreateTable(
                name: "ProjectUser",
                columns: table => new
                {
                    PeopleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectUser", x => new { x.PeopleId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_ProjectUser_People_PeopleId",
                        column: x => x.PeopleId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectUser_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleUser",
                columns: table => new
                {
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleUser", x => new { x.RolesId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RoleUser_People_UserId",
                        column: x => x.UserId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleUser_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copy data from old to new tables
            migrationBuilder.Sql(@"INSERT INTO ""ProjectUser"" (""PeopleId"", ""ProjectId"") 
                                  SELECT ""PeopleId"", ""ProjectId"" FROM ""PersonProject""");
            
            migrationBuilder.Sql(@"INSERT INTO ""RoleUser"" (""RolesId"", ""UserId"") 
                                  SELECT ""RolesId"", ""PersonId"" FROM ""PersonRole""");

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_ProjectUser_ProjectId",
                table: "ProjectUser",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleUser_UserId",
                table: "RoleUser",
                column: "UserId");

            // Now drop the old tables after data has been transferred
            migrationBuilder.DropTable(name: "PersonProject");
            migrationBuilder.DropTable(name: "PersonRole");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-create old tables
            migrationBuilder.CreateTable(
                name: "PersonProject",
                columns: table => new
                {
                    PeopleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonProject", x => new { x.PeopleId, x.ProjectId });
                    table.ForeignKey(
                        name: "FK_PersonProject_People_PeopleId",
                        column: x => x.PeopleId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonProject_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonRole",
                columns: table => new
                {
                    PersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonRole", x => new { x.PersonId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_PersonRole_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonRole_Roles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Copy data back from new to old tables
            migrationBuilder.Sql(@"INSERT INTO ""PersonProject"" (""PeopleId"", ""ProjectId"") 
                                  SELECT ""PeopleId"", ""ProjectId"" FROM ""ProjectUser""");
            
            migrationBuilder.Sql(@"INSERT INTO ""PersonRole"" (""PersonId"", ""RolesId"") 
                                  SELECT ""UserId"", ""RolesId"" FROM ""RoleUser""");

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_PersonProject_ProjectId",
                table: "PersonProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonRole_RolesId",
                table: "PersonRole",
                column: "RolesId");

            // Finally drop the new tables
            migrationBuilder.DropTable(name: "ProjectUser");
            migrationBuilder.DropTable(name: "RoleUser");
        }
    }
}
