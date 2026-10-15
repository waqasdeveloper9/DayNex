using DayNex.Domain.Common.Interface;
namespace DayNex.UserInfoService.Domain.Models;

/// <summary>Latest successful public-register snapshot; raw JSON preserves optional provider fields.</summary>
public sealed class CompanyProfile : IEntity
{
    public Guid Id { get; set; }
    public string CompanyNumber { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string? CompanyStatus { get; set; }
    public string? CompanyStatusDetail { get; set; }
    public string? CompanyType { get; set; }
    public string? Jurisdiction { get; set; }
    public DateOnly? DateOfCreation { get; set; }
    public DateOnly? DateOfCessation { get; set; }
    public bool? CanFile { get; set; }
    public bool? HasCharges { get; set; }
    public bool? HasInsolvencyHistory { get; set; }
    public bool? HasBeenLiquidated { get; set; }
    public bool? IsCommunityInterestCompany { get; set; }
    public DateOnly? AccountsDueOn { get; set; }
    public bool? AccountsOverdue { get; set; }
    public DateOnly? AccountsPeriodStartOn { get; set; }
    public DateOnly? AccountsPeriodEndOn { get; set; }
    public DateOnly? LastAccountsPeriodEndOn { get; set; }
    public DateOnly? ConfirmationStatementDueOn { get; set; }
    public DateOnly? ConfirmationStatementMadeUpTo { get; set; }
    public bool? ConfirmationStatementOverdue { get; set; }
    public string? Etag { get; set; }
    public DateTimeOffset RetrievedUtc { get; set; }
    public string RawResponseJson { get; set; } = "{}";
    public CompanyRegisteredOffice? RegisteredOffice { get; set; }
    public List<CompanySicCode> SicCodes { get; set; } = [];
    public List<CompanyPreviousName> PreviousNames { get; set; } = [];
}
public sealed class CompanyRegisteredOffice
{
    public Guid CompanyProfileId { get; set; }
    public string? Premises { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? CareOf { get; set; }
    public string? PoBox { get; set; }
    public string? Locality { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
}
public sealed class CompanySicCode
{
    public Guid CompanyProfileId { get; set; }
    public string Code { get; set; } = "";
}
public sealed class CompanyPreviousName
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyProfileId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? CeasedOn { get; set; }
}
