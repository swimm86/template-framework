using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.DatabaseUpgrade.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "person",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Идентификатор."),
                    name = table.Column<string>(type: "text", nullable: false, comment: "Имя"),
                    email = table.Column<string>(type: "text", nullable: false, comment: "Адрес электронной почты"),
                    hash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false, comment: "SHA-256 хэш")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person", x => x.id);
                },
                comment: "Таблица с сущностями \"Персона\".");

            migrationBuilder.CreateTable(
                name: "seed",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false, comment: "Уникальное наименование сида БД.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seed", x => x.id);
                },
                comment: "Таблица с сущностями \"Сид БД\".");

            migrationBuilder.CreateIndex(
                name: "IX_person_hash",
                table: "person",
                column: "hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "person");

            migrationBuilder.DropTable(
                name: "seed");
        }
    }
}
