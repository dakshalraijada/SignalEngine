using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Moves data source concept from Metric to Asset.
    /// - Metrics.Source (string) is removed - metrics describe WHAT is measured
    /// - Assets.DataSourceId (FK to LookupValues) is added - assets define WHERE data comes from
    /// 
    /// PREREQUISITE: DATA_SOURCE lookup type and values must exist before running.
    /// </summary>
    public partial class MoveDataSourceFromMetricToAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add DataSourceId column as nullable first
            migrationBuilder.AddColumn<int>(
                name: "DataSourceId",
                table: "Assets",
                type: "int",
                nullable: true);

            // Step 2: Set default value for existing assets (CUSTOM_API)
            // This uses a subquery to find the CUSTOM_API lookup value dynamically
            migrationBuilder.Sql(@"
                UPDATE Assets 
                SET DataSourceId = (
                    SELECT lv.Id 
                    FROM LookupValues lv 
                    INNER JOIN LookupTypes lt ON lv.LookupTypeId = lt.Id
                    WHERE lt.Code = 'DATA_SOURCE' AND lv.Code = 'CUSTOM_API'
                )
                WHERE DataSourceId IS NULL;
            ");

            // Step 3: Make column NOT NULL now that all rows have values
            migrationBuilder.AlterColumn<int>(
                name: "DataSourceId",
                table: "Assets",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Step 4: Create indexes for query performance
            migrationBuilder.CreateIndex(
                name: "IX_Assets_DataSourceId",
                table: "Assets",
                column: "DataSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_TenantId_DataSourceId",
                table: "Assets",
                columns: new[] { "TenantId", "DataSourceId" });

            // Step 5: Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Assets_LookupValues_DataSourceId",
                table: "Assets",
                column: "DataSourceId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Step 6: Drop Metrics.Source column (no longer needed)
            migrationBuilder.DropColumn(
                name: "Source",
                table: "Metrics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_LookupValues_DataSourceId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_DataSourceId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_TenantId_DataSourceId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "DataSourceId",
                table: "Assets");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Metrics",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
