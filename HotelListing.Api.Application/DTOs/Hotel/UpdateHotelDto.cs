using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.Application.DTOs.Hotel
{
    public class UpdateHotelDto : CreateHotelDto
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }
    }
}
