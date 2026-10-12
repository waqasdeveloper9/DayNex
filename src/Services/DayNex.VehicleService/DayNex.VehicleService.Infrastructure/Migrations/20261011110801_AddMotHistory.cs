using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.VehicleService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMotHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MotLastAttemptUtc",
                table: "UserVehicles",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotLookupStatus",
                table: "UserVehicles",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MotRetrievedUtc",
                table: "UserVehicles",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleMotHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Registration = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    Make = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirstUsedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FuelType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrimaryColour = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegistrationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ManufactureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ManufactureYear = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    EngineSize = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MotTestDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HasOutstandingRecall = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RetrievedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMotHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMotHistories_UserVehicles_Id",
                        column: x => x.Id,
                        principalTable: "UserVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMotTests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistrationAtTimeOfTest = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CompletedDate = table.Column<long>(type: "bigint", nullable: true),
                    TestResult = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OdometerValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OdometerUnit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    OdometerResultType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MotTestNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMotTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMotTests_VehicleMotHistories_HistoryId",
                        column: x => x.HistoryId,
                        principalTable: "VehicleMotHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleMotDefects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dangerous = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleMotDefects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleMotDefects_VehicleMotTests_TestId",
                        column: x => x.TestId,
                        principalTable: "VehicleMotTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMotDefects_TestId",
                table: "VehicleMotDefects",
                column: "TestId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleMotTests_HistoryId_CompletedDate",
                table: "VehicleMotTests",
                columns: new[] { "HistoryId", "CompletedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleMotDefects");

            migrationBuilder.DropTable(
                name: "VehicleMotTests");

            migrationBuilder.DropTable(
                name: "VehicleMotHistories");

            migrationBuilder.DropColumn(
                name: "MotLastAttemptUtc",
                table: "UserVehicles");

            migrationBuilder.DropColumn(
                name: "MotLookupStatus",
                table: "UserVehicles");

            migrationBuilder.DropColumn(
                name: "MotRetrievedUtc",
                table: "UserVehicles");
        }
    }
}
