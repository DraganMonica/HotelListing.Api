using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.Application.DTOs.Hotel
{
    public class CreateHotelDto
    {
        public required string Name { get; set; }

        [MaxLength(100)]
        public required string Address { get; set; }

        [Range(1, 5)]
        public double Rating { get; set; }
        [Range(1, int.MaxValue)]
        public decimal PerNightRate { get; set; }

        [Range(1, int.MaxValue)]
        public int CountryId { get; set; }
    }
}
