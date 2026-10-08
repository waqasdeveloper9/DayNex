using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.IdentityService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerOnboardingPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VehicleRegistration = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    CompanyNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HolidayRegion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_CustomerPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerPreferences");
        }
    }
}
