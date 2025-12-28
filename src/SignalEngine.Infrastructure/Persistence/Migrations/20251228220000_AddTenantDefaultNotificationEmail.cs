using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantDefaultNotificationEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultNotificationEmail",
                table: "Tenants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultNotificationEmail",
                table: "Tenants");
        }
    }
}
