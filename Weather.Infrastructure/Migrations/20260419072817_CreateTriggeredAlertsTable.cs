using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Weather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateTriggeredAlertsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alert_triggered",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlertSubscriptionId = table.Column<int>(type: "integer", nullable: false),
                    ObservationId = table.Column<long>(type: "bigint", nullable: false),
                    TemperatureCelsius = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alert_triggered", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alert_triggered_alert_subscriptions_AlertSubscriptionId",
                        column: x => x.AlertSubscriptionId,
                        principalTable: "alert_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_alert_triggered_weather_observations_ObservationId",
                        column: x => x.ObservationId,
                        principalTable: "weather_observations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alert_triggered_AlertSubscriptionId_ObservationId",
                table: "alert_triggered",
                columns: new[] { "AlertSubscriptionId", "ObservationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alert_triggered_ObservationId",
                table: "alert_triggered",
                column: "ObservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alert_triggered");
        }
    }
}


