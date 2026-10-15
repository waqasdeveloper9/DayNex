namespace DayNex.UserInfoService.Application;
public sealed record CompanyProfileDto(string CompanyName, string? Status, string? Type, string? Jurisdiction,
    DateOnly? DateOfCreation, DateOnly? AccountsDueOn, bool? AccountsOverdue,
    DateOnly? ConfirmationStatementDueOn, bool? ConfirmationStatementOverdue,
    CompanyOfficeDto? RegisteredOffice, IReadOnlyList<string> SicCodes, IReadOnlyList<PreviousCompanyNameDto> PreviousNames);
public sealed record CompanyOfficeDto(string? Premises, string? AddressLine1, string? AddressLine2,
    string? Locality, string? Region, string? Country, string? PostalCode);
public sealed record PreviousCompanyNameDto(string Name, DateOnly? EffectiveFrom, DateOnly? CeasedOn);
public sealed record CouncilDto(string Name, string HomepageUrl, string Tier, string Slug);
public sealed record LocationDto(string Postcode, string LookupStatus, DateTimeOffset? RetrievedUtc, CouncilDto? Council);
public sealed record UpdateLocationRequest(string? Postcode);
public sealed record CompleteUserInfoRequest(string? Postcode = null, IReadOnlyList<string>? VehicleRegistrations = null,
    IReadOnlyList<string>? CompanyNumbers = null);
