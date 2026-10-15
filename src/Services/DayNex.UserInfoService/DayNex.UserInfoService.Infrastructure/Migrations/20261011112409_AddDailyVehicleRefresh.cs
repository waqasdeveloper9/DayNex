using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.UserInfoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyVehicleRefresh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ScheduledAttemptUtcTicks",
                table: "UserVehicles",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleRefreshJobState",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntilUtcTicks = table.Column<long>(type: "bigint", nullable: false),
                    CompletedUtcTicks = table.Column<long>(type: "bigint", nullable: true),
                    Processed = table.Column<int>(type: "int", nullable: false),
                    Failed = table.Column<int>(type: "int", nullable: false),
                    Skipped = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRefreshJobState", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "VehicleRefreshJobState",
                columns: new[] { "Id", "CompletedUtcTicks", "Failed", "LeaseId", "LeaseUntilUtcTicks", "Processed", "Skipped" },
                values: new object[] { 1, null, 0, null, 0L, 0, 0 });

            migrationBuilder.CreateIndex(
                name: "IX_UserVehicles_ScheduledAttemptUtcTicks_Id",
                table: "UserVehicles",
                columns: new[] { "ScheduledAttemptUtcTicks", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VehicleRefreshJobState");

            migrationBuilder.DropIndex(
                name: "IX_UserVehicles_ScheduledAttemptUtcTicks_Id",
                table: "UserVehicles");

            migrationBuilder.DropColumn(
                name: "ScheduledAttemptUtcTicks",
                table: "UserVehicles");
        }
    }
}
