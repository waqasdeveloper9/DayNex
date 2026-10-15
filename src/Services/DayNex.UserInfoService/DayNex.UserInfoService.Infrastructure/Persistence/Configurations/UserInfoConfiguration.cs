using DayNex.UserInfoService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace DayNex.UserInfoService.Infrastructure;
public sealed class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> b)
    {
        b.ToTable("CompanyProfiles"); b.HasKey(x=>x.Id); b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.CompanyNumber).HasMaxLength(8); b.Property(x=>x.CompanyName).HasMaxLength(500);
        b.HasIndex(x=>x.AccountsDueOn); b.HasIndex(x=>x.ConfirmationStatementDueOn);
        b.HasOne(x=>x.RegisteredOffice).WithOne().HasForeignKey<CompanyRegisteredOffice>(x=>x.CompanyProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x=>x.SicCodes).WithOne().HasForeignKey(x=>x.CompanyProfileId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x=>x.PreviousNames).WithOne().HasForeignKey(x=>x.CompanyProfileId).OnDelete(DeleteBehavior.Cascade);
        b.Property(x=>x.RawResponseJson).IsRequired();
    }
}
public sealed class CompanyOfficeConfiguration : IEntityTypeConfiguration<CompanyRegisteredOffice>
{
    public void Configure(EntityTypeBuilder<CompanyRegisteredOffice> b) {b.ToTable("CompanyRegisteredOffices");b.HasKey(x=>x.CompanyProfileId);b.Property(x=>x.CompanyProfileId).ValueGeneratedNever();}
}
public sealed class CompanySicConfiguration : IEntityTypeConfiguration<CompanySicCode>
{
    public void Configure(EntityTypeBuilder<CompanySicCode> b) {b.ToTable("CompanySicCodes");b.HasKey(x=>new{x.CompanyProfileId,x.Code});b.Property(x=>x.Code).HasMaxLength(10);}
}
public sealed class CompanyPreviousNameConfiguration : IEntityTypeConfiguration<CompanyPreviousName>
{
    public void Configure(EntityTypeBuilder<CompanyPreviousName> b) {b.ToTable("CompanyPreviousNames");b.HasKey(x=>x.Id);}
}
public sealed class UserLocationConfiguration : IEntityTypeConfiguration<UserLocation>
{
    public void Configure(EntityTypeBuilder<UserLocation> b)
    {
        b.ToTable("UserLocations");b.HasKey(x=>x.Id);b.Property(x=>x.Id).ValueGeneratedNever();
        b.Property(x=>x.Postcode).HasMaxLength(8);b.Property(x=>x.LookupStatus).HasMaxLength(32);
        b.Property(x=>x.Revision).IsConcurrencyToken();
        b.HasOne(x=>x.Council).WithOne().HasForeignKey<UserCouncil>(x=>x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class UserCouncilConfiguration : IEntityTypeConfiguration<UserCouncil>
{
    public void Configure(EntityTypeBuilder<UserCouncil> b) {b.ToTable("UserCouncils");b.HasKey(x=>x.UserId);b.Property(x=>x.UserId).ValueGeneratedNever();}
}
