using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Booking;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Enums;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Application.Services;

public class BookingService(
    HotelListingDbContext context,
    IUsersService usersService,
    IMapper mapper) : IBookingService
{
    public async Task<Result<PageResult<GetBookingDto>>> GetBookingsForHotelAsync(int hotelId,PaginationParameters paginationParameters)
    {
        var hotelExists = await context.Hotels.AnyAsync(h => h.Id == hotelId);
        if (!hotelExists)
        {
            return Result<PageResult<GetBookingDto>>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel '{hotelId}' was not found."));
        }

        var bookings = await context.Bookings
            .AsNoTracking()
            .Where(b => b.HotelId == hotelId)
            .OrderBy(b => b.CheckIn)
            .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
            .ToPagedResultAsync(paginationParameters);

        return Result<PageResult<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<PageResult<GetBookingDto>>> GetUserBookingsForHotelAsync(int hotelId, PaginationParameters paginationParameters)
    {
        var userId = usersService.UserId;

        var hotelExists = await context.Hotels.AnyAsync(h => h.Id == hotelId);
        if (!hotelExists)
        {
            return Result<PageResult<GetBookingDto>>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel '{hotelId}' was not found."));
        }

        var bookings = await context.Bookings
            .AsNoTracking()
            .Where(b => b.HotelId == hotelId && b.UserId == userId)
            .OrderBy(b => b.CheckIn)
            .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
            .ToPagedResultAsync(paginationParameters);

        return Result<PageResult<GetBookingDto>>.Success(bookings);
    }

    public async Task<Result<GetBookingDto>> CreateBookingAsync(CreateBookingDto dto)
    {
        var userId = usersService.UserId;

        var overlaps = await IsOverlap(dto.HotelId, userId, dto.CheckIn, dto.CheckOut);

        if (overlaps)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, "The selected dates overlap with an existing booking."));

        var hotel = await context.Hotels.FirstOrDefaultAsync(h => h.Id == dto.HotelId);

        if (hotel is null)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.NotFound, $"Hotel '{dto.HotelId}' was not found."));

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;
        var totalPrice = hotel.PerNightRate * nights;

        //AUTO MAPPER(sunt match-uite: HotelId,  CheckIn, CheckOut, Guests )
        var booking = mapper.Map<Booking>(dto);

        //BUSINESS LOGIC (ce am pe Ignore)
        booking.UserId = userId;
        booking.TotalPrice = totalPrice;
        booking.Status = BookingStatusEnum.Pending;
        booking.Hotel = hotel;

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var created = mapper.Map<GetBookingDto>(booking);

        return Result<GetBookingDto>.Success(created);
    }

    public async Task<Result<GetBookingDto>> UpdateBookingAsync(int hotelId, int bookingId, UpdateBookingDto dto)
    {
        //verific user-cine vrea sa faca update-ul
        var userId = usersService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Validation, "User is required."));
        // trebuie sa stiu sigur ca nu exista date suprapuse
        var overlaps = await IsOverlap(hotelId, userId, dto.CheckIn, dto.CheckOut, bookingId);

        if (overlaps)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, "The selected dates overlap with an existing booking."));

        //cauta booking ul specific, caruia urmeaza sa i fac update
        var booking = await context.Bookings
            // fara asta, ar fii null Hotel
            .Include(b => b.Hotel)
            // verificare 
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.HotelId == hotelId &&
                b.UserId == userId);

        if (booking is null)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found"));

        if (booking.Status == BookingStatusEnum.Cancelled)
            return Result<GetBookingDto>
                .Failure(new Error(ErrorCodes.Conflict, "Cancelled bookings cannot be modified."));

        //AUTO MAPPER (ia din dto si pune in obiectul creat mai sus, care se updateaza)
        mapper.Map(dto, booking);
        var perNight = booking.Hotel!.PerNightRate;
        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;

        //BUSINESS LOGIC
        booking.TotalPrice = perNight * nights;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var updated = mapper.Map<GetBookingDto>(booking);

        return Result<GetBookingDto>.Success(updated);
    }

    public async Task<Result> CancelBookingAsync(int hotelId, int bookingId)
    {
        var userId = usersService.UserId;

        var booking = await context.Bookings
            .Include(b => b.Hotel)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.HotelId == hotelId &&
                b.UserId == userId);

        if (booking is null)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found"));

        if (booking.Status == BookingStatusEnum.Cancelled)
            return Result.Failure(new Error(ErrorCodes.Conflict, "Already cancelled."));

        booking.Status = BookingStatusEnum.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AdminCancelBookingAsync(int hotelId, int bookingId)
    {
        var userId = usersService.UserId;

        var isHotelAdminUser = await context.HotelAdmins
            .AnyAsync(q => q.UserId == userId && q.HotelId == hotelId);

        if (!isHotelAdminUser)
            return Result.Failure(new Error(ErrorCodes.Forbid, "Not admin."));

        var booking = await context.Bookings
            .Include(b => b.Hotel)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.HotelId == hotelId);

        if (booking is null)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

        if (booking.Status == BookingStatusEnum.Cancelled)
            return Result.Failure(new Error(ErrorCodes.Conflict, "Already cancelled."));

        booking.Status = BookingStatusEnum.Cancelled;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> AdminConfirmBookingAsync(int hotelId, int bookingId)
    {
        var userId = usersService.UserId;

        var isHotelAdminUser = await context.HotelAdmins
            .AnyAsync(q => q.UserId == userId && q.HotelId == hotelId);

        if (!isHotelAdminUser)
            return Result.Failure(new Error(ErrorCodes.Forbid, "Not admin."));

        var booking = await context.Bookings
            .Include(b => b.Hotel)
            .FirstOrDefaultAsync(b =>
                b.Id == bookingId &&
                b.HotelId == hotelId);

        if (booking is null)
            return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

        if (booking.Status == BookingStatusEnum.Cancelled)
            return Result.Failure(new Error(ErrorCodes.Conflict, "Already cancelled."));

        booking.Status = BookingStatusEnum.Confirmed;
        booking.UpdatedAtUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<bool> IsOverlap(int hotelId, string userId, DateOnly checkIn, DateOnly checkOut, int? bookingId=null)
    {
        var query= context.Bookings
            .Where(
                b => b.HotelId == hotelId
                && b.Status != BookingStatusEnum.Cancelled
                && checkIn < b.CheckOut
                && checkOut > b.CheckIn
                && b.UserId == userId)
            .AsQueryable();

        //verific daca se suprapune cu ALTE booking-uri, nu cu el insusi
        if (bookingId.HasValue)
        {
            query=query.Where(q => q.Id != bookingId.Value);
        }
        return await query.AnyAsync();
    }
}