using DayNex.UserInfoService.Domain.Models;

namespace DayNex.UserInfoService.Application;

/// <summary>Optional onboarding enrichment, reusable by any authenticated client.</summary>
public sealed class UserInfoOnboardingService(ICustomerAssetService assets, UserLocationService locations)
{
    public async Task<CustomerAssetsDto> CompleteAsync(CompleteUserInfoRequest input, CancellationToken token)
    {
        // Validate the complete batch before persisting any input.
        if ((input.VehicleRegistrations?.Count ?? 0) > 20 || (input.CompanyNumbers?.Count ?? 0) > 20)
            throw new ArgumentException("Submit no more than 20 references of each type at a time.");

        var registrations = (input.VehicleRegistrations ?? []).Select(UserVehicle.Normalize).Distinct().ToArray();
        var companies = (input.CompanyNumbers ?? []).Select(UserCompany.Normalize).Distinct().ToArray();
        if (input.Postcode is not null)
            UserLocation.Normalize(input.Postcode);

        // Separate providers can fail independently. Each reference is saved before enrichment.
        // A cancelled or interrupted batch can be retried without duplicating existing references.
        if (input.Postcode is not null)
            await locations.SaveAsync(input.Postcode, token);

        foreach (var company in companies)
            await assets.AddCompanyAsync(new(company), token);

        foreach (var registration in registrations)
            await assets.AddVehicleAsync(new(registration), token);

        return await assets.GetAsync(token);
    }
}
