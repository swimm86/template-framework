using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gpn.Template.DatabaseUpgrade.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneToOne",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneToOne", x => x.id);
                    table.ForeignKey(
                        name: "FK_OneToOne_Person_person_id",
                        column: x => x.person_id,
                        principalTable: "Person",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OneToMany",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    one_to_one_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneToMany", x => x.id);
                    table.ForeignKey(
                        name: "FK_OneToMany_OneToOne_one_to_one_id",
                        column: x => x.one_to_one_id,
                        principalTable: "OneToOne",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OneToMany_one_to_one_id",
                table: "OneToMany",
                column: "one_to_one_id");

            migrationBuilder.CreateIndex(
                name: "IX_OneToOne_person_id",
                table: "OneToOne",
                column: "person_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneToMany");

            migrationBuilder.DropTable(
                name: "OneToOne");
        }
    }
}
