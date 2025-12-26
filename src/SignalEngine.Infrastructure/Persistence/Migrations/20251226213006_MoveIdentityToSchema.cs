using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SignalEngine.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveIdentityToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.RenameTable(
                name: "OpenIddictTokens",
                newName: "OpenIddictTokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "OpenIddictScopes",
                newName: "OpenIddictScopes",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "OpenIddictAuthorizations",
                newName: "OpenIddictAuthorizations",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "OpenIddictApplications",
                newName: "OpenIddictApplications",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "identity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "OpenIddictTokens",
                schema: "identity",
                newName: "OpenIddictTokens");

            migrationBuilder.RenameTable(
                name: "OpenIddictScopes",
                schema: "identity",
                newName: "OpenIddictScopes");

            migrationBuilder.RenameTable(
                name: "OpenIddictAuthorizations",
                schema: "identity",
                newName: "OpenIddictAuthorizations");

            migrationBuilder.RenameTable(
                name: "OpenIddictApplications",
                schema: "identity",
                newName: "OpenIddictApplications");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "identity",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "identity",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "identity",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "identity",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "identity",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "identity",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "identity",
                newName: "AspNetRoleClaims");
        }
    }
}
