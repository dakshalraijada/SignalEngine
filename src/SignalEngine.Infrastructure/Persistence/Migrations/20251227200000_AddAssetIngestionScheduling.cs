using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalEngine.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddAssetIngestionScheduling : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add ingestion scheduling columns to Assets table
        migrationBuilder.AddColumn<int>(
            name: "IngestionIntervalSeconds",
            table: "Assets",
            type: "int",
            nullable: false,
            defaultValue: 60);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastIngestedAtUtc",
            table: "Assets",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextIngestionAtUtc",
            table: "Assets",
            type: "datetime2",
            nullable: true);

        // Index for finding assets due for ingestion
        // Efficient query: WHERE IsActive = 1 AND (NextIngestionAtUtc IS NULL OR NextIngestionAtUtc <= @now)
        migrationBuilder.CreateIndex(
            name: "IX_Assets_Ingestion_Due",
            table: "Assets",
            columns: new[] { "IsActive", "NextIngestionAtUtc" });

        // Index for grouping assets by DataSource during batch ingestion
        migrationBuilder.CreateIndex(
            name: "IX_Assets_DataSource_Identifier",
            table: "Assets",
            columns: new[] { "DataSourceId", "Identifier" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Assets_DataSource_Identifier",
            table: "Assets");

        migrationBuilder.DropIndex(
            name: "IX_Assets_Ingestion_Due",
            table: "Assets");

        migrationBuilder.DropColumn(
            name: "NextIngestionAtUtc",
            table: "Assets");

        migrationBuilder.DropColumn(
            name: "LastIngestedAtUtc",
            table: "Assets");

        migrationBuilder.DropColumn(
            name: "IngestionIntervalSeconds",
            table: "Assets");
    }
}
