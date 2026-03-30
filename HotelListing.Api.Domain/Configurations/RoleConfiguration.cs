
using HotelListing.Api.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Domain.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
                new IdentityRole
                {
                    Id= "4f7b3f8d-22bc-45bb-ae86-04446cd59d32",
                    Name=RoleNames.Administrator,
                    NormalizedName= RoleNames.Administrator.ToUpper()
                },
                new IdentityRole
                {
                    Id = "bbbd474b-82c9-4740-b4bb-d9a22cfbb886",
                    Name = RoleNames.User,
                    NormalizedName = RoleNames.User.ToUpper()
                },
                new IdentityRole
                {
                    Id = "e1cf31d3-2bb9-4428-8819-4d388458a72c",
                    Name = RoleNames.HotelAdmin,
                    NormalizedName = RoleNames.HotelAdmin.ToUpper()
                }
            );
    }
}
