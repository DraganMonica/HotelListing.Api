using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.Application.DTOs.Country
{
    public class UpdateCountryDto: CreateCountryDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }
    }
}
