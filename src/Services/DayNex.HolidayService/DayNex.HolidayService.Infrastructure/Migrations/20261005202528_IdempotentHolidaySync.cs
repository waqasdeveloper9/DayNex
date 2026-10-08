using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.HolidayService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdempotentHolidaySync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM dbo.BankHoliday GROUP BY Region, Date, Title COLLATE Latin1_General_100_BIN2 HAVING COUNT(*) > 1)
                    THROW 51001, 'Duplicate holidays exist. Review docs/MigrationPreflight.sql before applying this migration.', 1;
                """);
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "BankHoliday",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateIndex(
                name: "IX_BankHoliday_Region_Date_Title",
                table: "BankHoliday",
                columns: new[] { "Region", "Date", "Title" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BankHoliday_Region_Date_Title",
                table: "BankHoliday");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "BankHoliday",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldCollation: "Latin1_General_100_BIN2");
        }
    }
}
