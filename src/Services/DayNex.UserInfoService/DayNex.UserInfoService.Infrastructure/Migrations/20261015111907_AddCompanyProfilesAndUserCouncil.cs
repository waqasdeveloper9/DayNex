using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DayNex.UserInfoService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfilesAndUserCouncil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastAttemptUtc",
                table: "UserCompanies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LookupStatus",
                table: "UserCompanies",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RetrievedUtc",
                table: "UserCompanies",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyNumber = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CompanyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyStatusDetail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Jurisdiction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfCreation = table.Column<DateOnly>(type: "date", nullable: true),
                    DateOfCessation = table.Column<DateOnly>(type: "date", nullable: true),
                    CanFile = table.Column<bool>(type: "bit", nullable: true),
                    HasCharges = table.Column<bool>(type: "bit", nullable: true),
                    HasInsolvencyHistory = table.Column<bool>(type: "bit", nullable: true),
                    HasBeenLiquidated = table.Column<bool>(type: "bit", nullable: true),
                    IsCommunityInterestCompany = table.Column<bool>(type: "bit", nullable: true),
                    AccountsDueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    AccountsOverdue = table.Column<bool>(type: "bit", nullable: true),
                    AccountsPeriodStartOn = table.Column<DateOnly>(type: "date", nullable: true),
                    AccountsPeriodEndOn = table.Column<DateOnly>(type: "date", nullable: true),
                    LastAccountsPeriodEndOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmationStatementDueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmationStatementMadeUpTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ConfirmationStatementOverdue = table.Column<bool>(type: "bit", nullable: true),
                    Etag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetrievedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyProfiles_UserCompanies_Id",
                        column: x => x.Id,
                        principalTable: "UserCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Revision = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LookupStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastAttemptUtcTicks = table.Column<long>(type: "bigint", nullable: true),
                    RetrievedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyPreviousNames",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    CeasedOn = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyPreviousNames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyPreviousNames_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyRegisteredOffices",
                columns: table => new
                {
                    CompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Premises = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CareOf = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PoBox = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Locality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRegisteredOffices", x => x.CompanyProfileId);
                    table.ForeignKey(
                        name: "FK_CompanyRegisteredOffices_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySicCodes",
                columns: table => new
                {
                    CompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySicCodes", x => new { x.CompanyProfileId, x.Code });
                    table.ForeignKey(
                        name: "FK_CompanySicCodes_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCouncils",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HomepageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCouncils", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserCouncils_UserLocations_UserId",
                        column: x => x.UserId,
                        principalTable: "UserLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyPreviousNames_CompanyProfileId",
                table: "CompanyPreviousNames",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_AccountsDueOn",
                table: "CompanyProfiles",
                column: "AccountsDueOn");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_ConfirmationStatementDueOn",
                table: "CompanyProfiles",
                column: "ConfirmationStatementDueOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyPreviousNames");

            migrationBuilder.DropTable(
                name: "CompanyRegisteredOffices");

            migrationBuilder.DropTable(
                name: "CompanySicCodes");

            migrationBuilder.DropTable(
                name: "UserCouncils");

            migrationBuilder.DropTable(
                name: "CompanyProfiles");

            migrationBuilder.DropTable(
                name: "UserLocations");

            migrationBuilder.DropColumn(
                name: "LastAttemptUtc",
                table: "UserCompanies");

            migrationBuilder.DropColumn(
                name: "LookupStatus",
                table: "UserCompanies");

            migrationBuilder.DropColumn(
                name: "RetrievedUtc",
                table: "UserCompanies");
        }
    }
}
