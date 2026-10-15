using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.UserInfoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialVehicleAssets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastAttemptUtc = table.Column<long>(type: "bigint", nullable: true),
                    RetrievedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LookupStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Details_RegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_TaxStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_TaxDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Details_ArtEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Details_MotStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_MotExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Details_Make = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_MonthOfFirstDvlaRegistration = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Details_MonthOfFirstRegistration = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Details_YearOfManufacture = table.Column<int>(type: "int", nullable: true),
                    Details_EngineCapacity = table.Column<int>(type: "int", nullable: true),
                    Details_Co2Emissions = table.Column<int>(type: "int", nullable: true),
                    Details_FuelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_MarkedForExport = table.Column<bool>(type: "bit", nullable: true),
                    Details_Colour = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_TypeApproval = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_Wheelplan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_RevenueWeight = table.Column<int>(type: "int", nullable: true),
                    Details_RealDrivingEmissions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_DateOfLastV5CIssued = table.Column<DateOnly>(type: "date", nullable: true),
                    Details_EuroStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details_AutomatedVehicle = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanies_UserId_CompanyNumber",
                table: "UserCompanies",
                columns: new[] { "UserId", "CompanyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserVehicles_UserId_RegistrationNumber",
                table: "UserVehicles",
                columns: new[] { "UserId", "RegistrationNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCompanies");

            migrationBuilder.DropTable(
                name: "UserVehicles");
        }
    }
}
