using HotelListing.Api.Application.DTOs.Hotel;

namespace HotelListing.Api.Application.DTOs.Country
{
    public class GetCountryDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public List<GetHotelSlimDto>? Hotels { get; set; }
    }

}
