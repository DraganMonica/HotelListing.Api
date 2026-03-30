using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelListing.Api.Domain.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        
        builder.HasIndex(k => k.Key).IsUnique();
        
        builder.HasData(
                new ApiKey
                {
                    Id = 1,
                    AppName="app",
                    CreatedAtUtc = new DateTime(2026,01,01),
                    Key= "Kf9xP3LmQ7vZ2sA8dR6tYwU1nB4cHjX5"
                }
            );
    }
}
