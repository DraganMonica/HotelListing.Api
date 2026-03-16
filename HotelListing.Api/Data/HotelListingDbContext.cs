using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Data
{
    public class HotelListingDbContext: DbContext
    {
        public HotelListingDbContext(DbContextOptions<HotelListingDbContext> options):base(options)
        {

        }
        DbSet<Country> Countries { get; set; }
        DbSet<Hotel> Hotels { get; set; }
        
    }
}
