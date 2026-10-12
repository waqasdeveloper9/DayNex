using DayNex.Domain.Common.Specifications;
using DayNex.VehicleService.Domain.Models;

namespace DayNex.VehicleService.Application;
public sealed class VehiclesForUser : BaseSpecification<UserVehicle>
{
    public VehiclesForUser(Guid userId) : base(v => v.UserId == userId)
    {
        AddInclude(v => v.MotHistory!);
        AddOrderBy(v => v.RegistrationNumber);
        ApplyPaging(0, 100);
    }
}

public sealed class CompaniesForUser : BaseSpecification<UserCompany>
{
    public CompaniesForUser(Guid userId) : base(v => v.UserId == userId)
    {
        AddOrderBy(v => v.CompanyNumber);
        ApplyPaging(0, 100);
    }
}

public sealed class OwnedVehicle : BaseSpecification<UserVehicle>
{
    public OwnedVehicle(Guid userId, Guid id) : base(v => v.UserId == userId && v.Id == id)
    {
        AddInclude(v => v.MotHistory!);
    }
}
