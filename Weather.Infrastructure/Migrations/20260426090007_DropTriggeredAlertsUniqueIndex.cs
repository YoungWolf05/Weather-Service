using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Weather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTriggeredAlertsUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_alert_triggered_AlertSubscriptionId_ObservationId",
                table: "alert_triggered");

            migrationBuilder.CreateIndex(
                name: "IX_alert_triggered_AlertSubscriptionId_ObservationId",
                table: "alert_triggered",
                columns: new[] { "AlertSubscriptionId", "ObservationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_alert_triggered_AlertSubscriptionId_ObservationId",
                table: "alert_triggered");

            migrationBuilder.CreateIndex(
                name: "IX_alert_triggered_AlertSubscriptionId_ObservationId",
                table: "alert_triggered",
                columns: new[] { "AlertSubscriptionId", "ObservationId" },
                unique: true);
        }
    }
}
