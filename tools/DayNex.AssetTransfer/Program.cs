using System.Data;
using DayNex.VehicleService.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
if(args.Length!=1 || args[0]!="--execute") {
 Console.WriteLine("No database accessed. After reviewing the guide and applying only the Vehicle schema, run with --execute to transfer and clear legacy references.");
 return;
}
string Connection(string secretId)=>new ConfigurationBuilder().AddUserSecrets(secretId).Build().GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Missing database secret.");
await using var source=new SqlConnection(Connection("DayNex-IdentityService-Development"));
await using var target=new SqlConnection(Connection("DayNex-VehicleService-Development"));
await source.OpenAsync();await target.OpenAsync();
var rows=new List<(Guid UserId,string? Vehicle,string? Company)>();
await using(var query=new SqlCommand("SELECT UserId,VehicleRegistration,CompanyNumber FROM CustomerPreferences WHERE VehicleRegistration IS NOT NULL OR CompanyNumber IS NOT NULL",source))
await using(var reader=await query.ExecuteReaderAsync())
 while(await reader.ReadAsync()) rows.Add((reader.GetGuid(0),reader.IsDBNull(1)?null:reader.GetString(1),reader.IsDBNull(2)?null:reader.GetString(2)));
// Validate everything before any writes. Invalid legacy values must be corrected explicitly.
foreach(var row in rows) {
 if(!string.IsNullOrWhiteSpace(row.Vehicle)) _=UserVehicle.Normalize(row.Vehicle);
 if(!string.IsNullOrWhiteSpace(row.Company)) _=UserCompany.Create(row.UserId,row.Company,DateTimeOffset.UtcNow);
}
foreach(var row in rows) {
 await using var transaction=(SqlTransaction)await target.BeginTransactionAsync(IsolationLevel.Serializable);
 if(!string.IsNullOrWhiteSpace(row.Vehicle)) {
  await using var insert=new SqlCommand("""
   IF NOT EXISTS(SELECT 1 FROM UserVehicles WHERE UserId=@user AND RegistrationNumber=@number)
    INSERT INTO UserVehicles(Id,UserId,RegistrationNumber,CreatedUtc,LookupStatus)
    VALUES(@id,@user,@number,@now,'Pending');
   """,target,transaction);
  insert.Parameters.Add("@user",SqlDbType.UniqueIdentifier).Value=row.UserId;
  insert.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=Guid.NewGuid();
  insert.Parameters.Add("@number",SqlDbType.NVarChar,7).Value=UserVehicle.Normalize(row.Vehicle);
  insert.Parameters.Add("@now",SqlDbType.DateTimeOffset).Value=DateTimeOffset.UtcNow;
  await insert.ExecuteNonQueryAsync();
 }
 if(!string.IsNullOrWhiteSpace(row.Company)) {
  await using var insert=new SqlCommand("""
   IF NOT EXISTS(SELECT 1 FROM UserCompanies WHERE UserId=@user AND CompanyNumber=@number)
    INSERT INTO UserCompanies(Id,UserId,CompanyNumber,CreatedUtc) VALUES(@id,@user,@number,@now);
   """,target,transaction);
  insert.Parameters.Add("@user",SqlDbType.UniqueIdentifier).Value=row.UserId;
  insert.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=Guid.NewGuid();
  insert.Parameters.Add("@number",SqlDbType.NVarChar,8).Value=row.Company.Trim().ToUpperInvariant();
  insert.Parameters.Add("@now",SqlDbType.DateTimeOffset).Value=DateTimeOffset.UtcNow;
  await insert.ExecuteNonQueryAsync();
 }
 await transaction.CommitAsync();
 // Clear only the exact source values durably copied. A crash before this is safe to retry.
 await using var clear=new SqlCommand("""
  UPDATE CustomerPreferences SET VehicleRegistration=NULL,CompanyNumber=NULL
  WHERE UserId=@user AND (VehicleRegistration=@vehicle OR VehicleRegistration IS NULL AND @vehicle IS NULL)
  AND (CompanyNumber=@company OR CompanyNumber IS NULL AND @company IS NULL);
  """,source);
 clear.Parameters.Add("@user",SqlDbType.UniqueIdentifier).Value=row.UserId;
 clear.Parameters.Add("@vehicle",SqlDbType.NVarChar,12).Value=(object?)row.Vehicle ?? DBNull.Value;
 clear.Parameters.Add("@company",SqlDbType.NVarChar,8).Value=(object?)row.Company ?? DBNull.Value;
 if(await clear.ExecuteNonQueryAsync()!=1) throw new InvalidOperationException("Legacy source changed; stop and review before rerunning.");
}
Console.WriteLine($"Transferred {rows.Count} legacy rows. No provider lookups were made.");
