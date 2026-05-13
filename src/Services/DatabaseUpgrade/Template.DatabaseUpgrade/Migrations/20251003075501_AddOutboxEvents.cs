using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.DatabaseUpgrade.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Тип события для маршрутизации."),
                    event_data = table.Column<string>(type: "text", nullable: false, comment: "JSON-данные события."),
                    correlation_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Идентификатор корреляции для связывания событий."),
                    status = table.Column<int>(type: "integer", nullable: false, comment: "Текущий статус события."),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, comment: "Время создания события."),
                    processed_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Время успешной обработки события."),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Количество попыток обработки."),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "Сообщение об ошибке при неудачной обработке."),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true, comment: "URL для HTTP-запроса."),
                    http_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "HTTP-метод (GET, POST, PUT, DELETE)."),
                    headers_json = table.Column<string>(type: "text", nullable: true, comment: "JSON-сериализованные HTTP-заголовки."),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Тип контента HTTP-запроса."),
                    timeout_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 100, comment: "Таймаут HTTP-запроса в секундах."),
                    max_retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 5, comment: "Максимальное количество попыток."),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Время следующей попытки обработки."),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Приоритет события (0 - наивысший)."),
                    idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Ключ идемпотентности для предотвращения дублирования."),
                    trace_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Идентификатор трейса для распределенного трейсинга."),
                    tenant_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Идентификатор арендатора для мультитенантности."),
                    lock_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Идентификатор блокировки для конкурентной обработки."),
                    lock_expires_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Время истечения блокировки.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.id);
                },
                comment: "Таблица событий Outbox для надежной доставки сообщений.");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_correlation_id",
                table: "outbox_events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_created_at",
                table: "outbox_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_idempotency_key",
                table: "outbox_events",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_next_attempt_at",
                table: "outbox_events",
                column: "next_attempt_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_priority",
                table: "outbox_events",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_status",
                table: "outbox_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_events_status_next_attempt",
                table: "outbox_events",
                columns: new[] { "status", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_events");
        }
    }
}
