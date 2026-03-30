namespace HotelListing.Api.Application.DTOs.Hotel
{
    public record GetHotelDto
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public double Rating { get; init; }
        public decimal PerNightRate { get; init; }
        public int CountryId { get; init; }
        public string Country { get; init; } = string.Empty;
    }
}
