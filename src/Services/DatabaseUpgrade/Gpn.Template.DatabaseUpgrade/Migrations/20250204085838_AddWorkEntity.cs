using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gpn.Template.DatabaseUpgrade.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Work", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PersonWork",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonWork", x => x.id);
                    table.ForeignKey(
                        name: "FK_PersonWork_Person_person_id",
                        column: x => x.person_id,
                        principalTable: "Person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonWork_Work_work_id",
                        column: x => x.work_id,
                        principalTable: "Work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonWork_person_id",
                table: "PersonWork",
                column: "person_id");

            migrationBuilder.CreateIndex(
                name: "IX_PersonWork_work_id",
                table: "PersonWork",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonWork");

            migrationBuilder.DropTable(
                name: "Work");
        }
    }
}
