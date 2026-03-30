using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.Application.DTOs.Booking;

public class CreateBookingDto:IValidatableObject
{
    [Required]
    public int HotelId { get; set; }
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    [Required]
    [Range(1,10)]
    public int Guests { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckOut <= CheckIn)
        {
            yield return new ValidationResult(
                "Check-out must be after check-in.",
                new[] { nameof(CheckOut), nameof(CheckIn) }
            );
        }
    }
}
