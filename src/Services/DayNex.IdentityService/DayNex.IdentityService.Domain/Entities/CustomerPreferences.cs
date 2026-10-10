using System.Text.RegularExpressions;
namespace DayNex.IdentityService.Domain.Entities;
public sealed partial class CustomerPreferences {
 public Guid UserId {get;private set;}
 public string Postcode {get;private set;}="";
 public string HolidayRegion {get;private set;}="EnglandAndWales";
 public DateTime UpdatedUtc {get;private set;}
 private CustomerPreferences() {}
 public static CustomerPreferences Create(Guid userId,string? postcode,string? region,DateTime now) {
  if(userId==Guid.Empty) throw new ArgumentException("A customer is required.");
  var normalized=(postcode??"").Trim().ToUpperInvariant();
  if(normalized.Length>10 || normalized.Length>0 && !PostcodePattern().IsMatch(normalized))
   throw new ArgumentException("Enter a valid UK postcode or leave it empty.");
  if(region is not ("EnglandAndWales" or "Scotland" or "NorthernIreland")) throw new ArgumentException("Choose a supported holiday region.");
  normalized=string.Concat(normalized.Where(c=>!char.IsWhiteSpace(c)));
  return new() {UserId=userId,Postcode=normalized.Length==0?"":normalized.Insert(normalized.Length-3," "),HolidayRegion=region,UpdatedUtc=now};
 }
 [GeneratedRegex(@"\A(?:[A-Z]{1,2}[0-9][A-Z0-9]?\s*[0-9][A-Z]{2}|GIR\s*0AA)\z",RegexOptions.CultureInvariant)]
 private static partial Regex PostcodePattern();
}