using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SchemaRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Signals_TriggeredAt",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IsSent",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Metrics_AssetId_Name",
                table: "Metrics");

            migrationBuilder.DropIndex(
                name: "IX_Metrics_Timestamp",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "Signals");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "Signals");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "Signals");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "Metrics");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Metrics",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LookupValues",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "LookupValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "LookupValues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "LookupValues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "LookupTypes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "LookupTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "LookupTypes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "LookupTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MetricData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MetricId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MetricData_Metrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "Metrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MetricData_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalResolutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SignalId = table.Column<int>(type: "int", nullable: false),
                    ResolutionStatusId = table.Column<int>(type: "int", nullable: false),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalResolutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignalResolutions_LookupValues_ResolutionStatusId",
                        column: x => x.ResolutionStatusId,
                        principalTable: "LookupValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalResolutions_Signals_SignalId",
                        column: x => x.SignalId,
                        principalTable: "Signals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalResolutions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_PlanId",
                table: "Tenants",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantTypeId",
                table: "Tenants",
                column: "TenantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_AssetId",
                table: "Signals",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_TenantId_TriggeredAt",
                table: "Signals",
                columns: new[] { "TenantId", "TriggeredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_OperatorId",
                table: "Rules",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Rules_SeverityId",
                table: "Rules",
                column: "SeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_PlanCodeId",
                table: "Plans",
                column: "PlanCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ChannelTypeId",
                table: "Notifications",
                column: "ChannelTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TenantId_IsSent_CreatedAt",
                table: "Notifications",
                columns: new[] { "TenantId", "IsSent", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_AssetId_Name",
                table: "Metrics",
                columns: new[] { "AssetId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_MetricTypeId",
                table: "Metrics",
                column: "MetricTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTypeId",
                table: "Assets",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricData_MetricId_Timestamp",
                table: "MetricData",
                columns: new[] { "MetricId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MetricData_TenantId",
                table: "MetricData",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MetricData_Timestamp",
                table: "MetricData",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SignalResolutions_ResolutionStatusId",
                table: "SignalResolutions",
                column: "ResolutionStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalResolutions_SignalId",
                table: "SignalResolutions",
                column: "SignalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalResolutions_TenantId",
                table: "SignalResolutions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_LookupValues_AssetTypeId",
                table: "Assets",
                column: "AssetTypeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Metrics_LookupValues_MetricTypeId",
                table: "Metrics",
                column: "MetricTypeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_LookupValues_ChannelTypeId",
                table: "Notifications",
                column: "ChannelTypeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_LookupValues_PlanCodeId",
                table: "Plans",
                column: "PlanCodeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_LookupValues_EvaluationFrequencyId",
                table: "Rules",
                column: "EvaluationFrequencyId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_LookupValues_OperatorId",
                table: "Rules",
                column: "OperatorId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Rules_LookupValues_SeverityId",
                table: "Rules",
                column: "SeverityId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Signals_Assets_AssetId",
                table: "Signals",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Signals_LookupValues_SignalStatusId",
                table: "Signals",
                column: "SignalStatusId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_LookupValues_TenantTypeId",
                table: "Tenants",
                column: "TenantTypeId",
                principalTable: "LookupValues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenants_Plans_PlanId",
                table: "Tenants",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_LookupValues_AssetTypeId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Metrics_LookupValues_MetricTypeId",
                table: "Metrics");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_LookupValues_ChannelTypeId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Plans_LookupValues_PlanCodeId",
                table: "Plans");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_LookupValues_EvaluationFrequencyId",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_LookupValues_OperatorId",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_Rules_LookupValues_SeverityId",
                table: "Rules");

            migrationBuilder.DropForeignKey(
                name: "FK_Signals_Assets_AssetId",
                table: "Signals");

            migrationBuilder.DropForeignKey(
                name: "FK_Signals_LookupValues_SignalStatusId",
                table: "Signals");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_LookupValues_TenantTypeId",
                table: "Tenants");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Plans_PlanId",
                table: "Tenants");

            migrationBuilder.DropTable(
                name: "MetricData");

            migrationBuilder.DropTable(
                name: "SignalResolutions");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_PlanId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_TenantTypeId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Signals_AssetId",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_Signals_TenantId_TriggeredAt",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_Rules_OperatorId",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_Rules_SeverityId",
                table: "Rules");

            migrationBuilder.DropIndex(
                name: "IX_Plans_PlanCodeId",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ChannelTypeId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TenantId_IsSent_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Metrics_AssetId_Name",
                table: "Metrics");

            migrationBuilder.DropIndex(
                name: "IX_Metrics_MetricTypeId",
                table: "Metrics");

            migrationBuilder.DropIndex(
                name: "IX_Assets_AssetTypeId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Metrics");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LookupValues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupValues");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "LookupValues");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "LookupValues");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "LookupTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "LookupTypes");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "LookupTypes");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "LookupTypes");

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "Signals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAt",
                table: "Signals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedBy",
                table: "Signals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "Metrics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "Value",
                table: "Metrics",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Signals_TriggeredAt",
                table: "Signals",
                column: "TriggeredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IsSent",
                table: "Notifications",
                column: "IsSent");

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_AssetId_Name",
                table: "Metrics",
                columns: new[] { "AssetId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Metrics_Timestamp",
                table: "Metrics",
                column: "Timestamp");
        }
    }
}
