using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Application.Services
{
    public class HotelsService(HotelListingDbContext context,IMapper mapper): IHotelsService
    {
        public async Task<Result<PageResult<GetHotelDto>>> GetHotelsAsync(PaginationParameters paginationParameters, HotelFilterParameters filters)
        {
            var query = context.Hotels.AsQueryable();
            
            if (filters.CountryId.HasValue)
            {
                query = query.Where(h => h.CountryId == filters.CountryId.Value);
            }
            if (filters.MinRating.HasValue)
            {
                query = query.Where(h => h.Rating >= filters.MinRating.Value);
            }
            if (filters.MaxRating.HasValue)
            {
                query = query.Where(h => h.Rating <= filters.MaxRating.Value);
            }
            if (filters.MinPrice.HasValue)
            {
                query = query.Where(h => h.PerNightRate >= filters.MinPrice.Value);
            }
            if (filters.MaxPrice.HasValue)
            {
                query = query.Where(h => h.PerNightRate <= filters.MaxPrice.Value);
            }
            //returneaza doar pe cele care  contin textul- exemplu: cele care au Cluj in Address
            if (!string.IsNullOrWhiteSpace(filters.Location))
            {
                query = query.Where(h => h.Address.Contains(filters.Location));
            }
            //same, daca apare in oricare: in Name or Address
            if (!string.IsNullOrWhiteSpace(filters.Search))
            {
                query = query.Where(h => h.Name.Contains(filters.Search)|| h.Address.Contains(filters.Search));
            }

            query = filters.SortBy?.ToLower() switch
            {
                "name" => filters.SortDescending
                    ? query.OrderByDescending(h => h.Name)
                    : query.OrderBy(h => h.Name),

                "rating" => filters.SortDescending
                    ? query.OrderByDescending(h => h.Rating)
                    : query.OrderBy(h => h.Rating),

                "price" => filters.SortDescending
                    ? query.OrderByDescending(h => h.PerNightRate)
                    : query.OrderBy(h => h.PerNightRate),

                _ => query.OrderBy(h => h.Name)
            };


            var hotels = await query
                .AsNoTracking()
                .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                .ToPagedResultAsync(paginationParameters);

            return Result<PageResult<GetHotelDto>>.Success(hotels);
        }

        public async Task<Result<GetHotelDto>> GetHotelAsync(int id)
        {
            var hotel = await context.Hotels
                .AsNoTracking()
                .Where(h => h.Id == id)
                .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return hotel is null
                 ? Result<GetHotelDto>.NotFound()
                 : Result<GetHotelDto>.Success(hotel);
        }

        public async Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto)
        {
            try
            {
                var exists = await 
                    HotelExistsAsync(hotelDto.Name);
                if (exists)
                    return Result<GetHotelDto>.Failure(new Error(ErrorCodes.Conflict, $"Hotel with the name '{hotelDto.Name}' already exists."));
                // asta inlocuieste ce e mai jos
                var hotel=mapper.Map<Hotel>(hotelDto);
                //var hotel = new Hotel
                //{
                //    Name = hotelDto.Name,
                //    Address = hotelDto.Address,
                //    Rating = hotelDto.Rating,
                //    CountryId = hotelDto.CountryId
                //};
                context.Hotels.Add(hotel);
                await context.SaveChangesAsync();

                var resultDto = await context.Hotels
                    .Where(h => h.Id == hotel.Id)
                    .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();

                return Result<GetHotelDto>.Success(resultDto!);
            }
            catch (Exception)
            {
                return Result<GetHotelDto>.Failure();
            }
        }

        public async Task<Result> UpdateHotelAsync(int id, UpdateHotelDto hotelDto)
        {

            try
            {
                //mai intai gasesc hotelul existent
                var hotel = await context.Hotels.FindAsync(id) ?? throw new KeyNotFoundException("Hotel not found");
                if (hotel is null)
                    return Result.NotFound(new Error(ErrorCodes.NotFound, $"Hotel '{id}' was not found"));

                var duplicateName = await HotelExistsAsync(hotelDto.Name);
                if (duplicateName)
                    return Result.Failure(new Error(ErrorCodes.Conflict, $"Hotel with the name '{hotelDto.Name}' already exists."));

                //n are <T>, deci asa arata mapper ul pt update
                mapper.Map(hotelDto, hotel);

                //hotel.Name = hotelDto.Name;
                //hotel.Address = hotelDto.Address;
                //hotel.Rating = hotelDto.Rating;
                //hotel.CountryId = hotelDto.CountryId;

                context.Hotels.Update(hotel);
                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception)
            {
                return Result.Failure();
            }
        }

        public async Task<Result> DeleteHotelAsync(int id)
        {
            try
            {
                var hotel = await context.Hotels.FindAsync(id);
                if (hotel is null)
                    return Result.NotFound(new Error(ErrorCodes.NotFound, $"Hotel '{id}' was not found"));

                context.Hotels.Remove(hotel);
                await context.SaveChangesAsync();

                return Result.Success();
            }
            catch (Exception)
            {
                return Result.Failure();
            }
        }

        public async Task<bool> HotelExistsAsync(int id)
        {
            return await context.Hotels.AnyAsync(e => e.Id == id);
        }
        public async Task<bool> HotelExistsAsync(string name)
        {
            return await context.Hotels.AnyAsync(e => e.Name == name);
        }

        
    }
}
