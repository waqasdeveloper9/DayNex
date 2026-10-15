using System.Globalization;
using System.Text.Json;
using DayNex.UserInfoService.Domain.Models;
namespace DayNex.UserInfoService.Infrastructure;
internal static class CompanyProfileResponse
{
    internal static CompanyProfile Map(JsonElement json, string expected)
    {
        if (json.ValueKind != JsonValueKind.Object) throw new JsonException();
        var number = Text(json,"company_number");
        if (!string.Equals(number,expected,StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(Text(json,"company_name")))
            throw new JsonException("Company response does not match the requested reference.");
        var accounts = Child(json,"accounts"); var next = Child(accounts,"next_accounts");
        var statement = Child(json,"confirmation_statement");
        var profile = new CompanyProfile {
            CompanyNumber=expected, CompanyName=Text(json,"company_name")!, CompanyStatus=Text(json,"company_status"),
            CompanyStatusDetail=Text(json,"company_status_detail"), CompanyType=Text(json,"type"), Jurisdiction=Text(json,"jurisdiction"),
            DateOfCreation=Date(json,"date_of_creation"), DateOfCessation=Date(json,"date_of_cessation"),
            CanFile=Boolean(json,"can_file"), HasCharges=Boolean(json,"has_charges"), HasBeenLiquidated=Boolean(json,"has_been_liquidated"),
            HasInsolvencyHistory=Boolean(json,"has_insolvency_history"), IsCommunityInterestCompany=Boolean(json,"is_community_interest_company"),
            AccountsDueOn=Date(next,"due_on") ?? Date(accounts,"next_due"), AccountsOverdue=Boolean(next,"overdue") ?? Boolean(accounts,"overdue"),
            AccountsPeriodStartOn=Date(next,"period_start_on"), AccountsPeriodEndOn=Date(next,"period_end_on") ?? Date(accounts,"next_made_up_to"),
            LastAccountsPeriodEndOn=Date(Child(accounts,"last_accounts"),"period_end_on"),
            ConfirmationStatementDueOn=Date(statement,"next_due"), ConfirmationStatementMadeUpTo=Date(statement,"next_made_up_to"),
            ConfirmationStatementOverdue=Boolean(statement,"overdue"), Etag=Text(json,"etag"), RawResponseJson=json.GetRawText()
        };
        var address = Child(json,"registered_office_address");
        if (address.ValueKind == JsonValueKind.Object) profile.RegisteredOffice = new() {
            Premises=Text(address,"premises"), AddressLine1=Text(address,"address_line_1"), AddressLine2=Text(address,"address_line_2"),
            CareOf=Text(address,"care_of"), PoBox=Text(address,"po_box"), Locality=Text(address,"locality"), Region=Text(address,"region"),
            Country=Text(address,"country"), PostalCode=Text(address,"postal_code")
        };
        var codes=Child(json,"sic_codes");
        if (codes.ValueKind == JsonValueKind.Array) profile.SicCodes=codes.EnumerateArray().Select(x=>x.GetString() ?? "")
            .Distinct().Select(x=>new CompanySicCode {Code=x}).ToList();
        var names=Child(json,"previous_company_names");
        if (names.ValueKind == JsonValueKind.Array) profile.PreviousNames=names.EnumerateArray().Select(x=>new CompanyPreviousName {
            Name=Text(x,"name") ?? "", EffectiveFrom=Date(x,"effective_from"), CeasedOn=Date(x,"ceased_on")
        }).ToList();
        return profile;
    }
    private static JsonElement Child(JsonElement json,string name) => json.ValueKind==JsonValueKind.Object && json.TryGetProperty(name,out var child) ? child : default;
    private static string? Text(JsonElement json,string name) {var v=Child(json,name); return v.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : v.GetString();}
    private static bool? Boolean(JsonElement json,string name) {var v=Child(json,name); return v.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : v.GetBoolean();}
    private static DateOnly? Date(JsonElement json,string name) {
        var value=Text(json,name); if(value is null) return null;
        if(!DateOnly.TryParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)) throw new JsonException("Invalid date.");
        return date;
    }
}
