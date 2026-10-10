using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.IdentityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveAssetReferencesOutOfIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Refuse destructive cleanup until every old reference has been copied and cleared.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [CustomerPreferences]
                    WHERE [VehicleRegistration] IS NOT NULL OR [CompanyNumber] IS NOT NULL)
                    THROW 51000, 'Transfer legacy references to VehicleService before removing Identity columns.', 1;
                """);
            migrationBuilder.DropColumn(
                name: "CompanyNumber",
                table: "CustomerPreferences");

            migrationBuilder.DropColumn(
                name: "VehicleRegistration",
                table: "CustomerPreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CompanyNumber",
                table: "CustomerPreferences",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleRegistration",
                table: "CustomerPreferences",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);
        }
    }
}
