using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Weather.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateWeatherForecastsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "weather_forecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ForecastType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ForecastText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TempLowCelsius = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    TempHighCelsius = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    HumidityLowPct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    HumidityHighPct = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    WindDirection = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    WindSpeedLowKmh = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    WindSpeedHighKmh = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weather_forecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weather_forecasts_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecasts_LocationId_IssuedAt_ValidFrom_ForecastType",
                table: "weather_forecasts",
                columns: new[] { "LocationId", "IssuedAt", "ValidFrom", "ForecastType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weather_forecasts_ValidFrom_LocationId",
                table: "weather_forecasts",
                columns: new[] { "ValidFrom", "LocationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "weather_forecasts");
        }
    }
}


