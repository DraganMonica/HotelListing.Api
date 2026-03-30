using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using System.CodeDom;
using System.Diagnostics.Metrics;

namespace HotelListing.Api.Application.Services;

public class CountriesService(HotelListingDbContext context, IMapper mapper, IMemoryCache cache) : ICountriesService
{
    private const string CountryListCacheName = "countries_list_";
    private object CountrySingleCacheName= "country_";

    public async Task<Result<PageResult<GetCountriesDto>>> GetCountriesAsync(PaginationParameters paginationParameters, CountryFilterParameters? filters)
    {
        var searchTerm=filters?.Search?.Trim().ToLowerInvariant() ?? string.Empty;
        var cacheKey = $"{CountryListCacheName}{searchTerm}";

        if (!cache.TryGetValue(cacheKey, out PageResult<GetCountriesDto>? countries)) 
        {
            var query = context.Countries.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters?.Search))
            {
                //fara extra spatii
                var term = filters.Search.Trim();
                // exact ca in SQL-unde apare, se ia in considerare
                query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%") || EF.Functions.Like(c.ShortName, $"%{term}%"));
            }

            countries = await query
                .AsNoTracking()
                .ProjectTo<GetCountriesDto>(mapper.ConfigurationProvider)
                .ToPagedResultAsync(paginationParameters);

            var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            cache.Set(cacheKey, countries, cacheOptions);
        }

        return Result<PageResult<GetCountriesDto>>.Success(countries);
    }

    public async Task<Result<GetCountryDto>> GetCountryAsync(int id)
    {
        //check the cache
        var cacheKey = $"{CountrySingleCacheName}{id}";
        // daca gasesc cache, il pun in contry, daca nu, cont e in {...}
        if (!cache.TryGetValue(cacheKey, out GetCountryDto? country))
        {
            country = await context.Countries
                   .AsNoTracking()
                   .Where(q => q.CountryId == id)
                   //country are hotels, iar GetCountryDto are de tip GetHotelSlimDto, asa ca adaug in mapping si partea cu aceasta potrivire
                   //ce a fost manual se inlocuieste cu asta
                   .ProjectTo<GetCountryDto>(mapper.ConfigurationProvider)
                   .FirstOrDefaultAsync();

            if(country is not null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                cache.Set(cacheKey, country, cacheOptions);
            }
        }
        
        return country is null
            ? Result<GetCountryDto>.Failure(new Error(ErrorCodes.NotFound,$"Country '{id}' was not found."))
            : Result<GetCountryDto>.Success(country);
    }
    public async Task<Result<GetCountryHotelsDto>> GetCountryHotelsAsync(
    int countryId,
    PaginationParameters paginationParameters, CountryFilterParameters filters)
    {
        var exists = await CountryExistsAsync(countryId);
        if (!exists)
        {
            return Result<GetCountryHotelsDto>.Failure(
                new Error(ErrorCodes.NotFound, $"Country '{countryId}' was not found."));
        }

        var countryName = await context.Countries
            .Where(q => q.CountryId == countryId)
            .Select(q => q.Name)
            .SingleAsync();

        var hotelsQuery = context.Hotels
            .Where(h => h.CountryId == countryId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            hotelsQuery = hotelsQuery.Where(h => EF.Functions.Like(h.Name, $"%{term}%"));
        }

        hotelsQuery = (filters.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Name) : hotelsQuery.OrderBy(h => h.Name),
            "rating" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Rating) : hotelsQuery.OrderBy(h => h.Rating),
            _ => hotelsQuery.OrderBy(h => h.Name)
        };


        var pagedHotels = await hotelsQuery
            .Where(h => h.CountryId == countryId)
            .OrderBy(h => h.Name) 
            .ProjectTo<GetHotelSlimDto>(mapper.ConfigurationProvider)
            .ToPagedResultAsync(paginationParameters);

        var result = new GetCountryHotelsDto
        {
            Id = countryId,
            Name = countryName,
            Hotels = pagedHotels
        };

        return Result<GetCountryHotelsDto>.Success(result);
    }

    public async Task<Result<GetCountryDto>> CreateCountryAsync(CreateCountryDto createDto)
    {
        try
        {
            var exists = await CountryExistsAsync(createDto.Name);
            if (exists)
            {
                return Result<GetCountryDto>.Failure(new Error(ErrorCodes.Conflict, $"Country with the name '{createDto.Name}' already exists."));
            }
            // se iau datele de tip createdto si se creeaza un obiect de tip Country
            var country = mapper.Map<Country>(createDto);

            context.Countries.Add(country);
            await context.SaveChangesAsync();

            // se iau datele de tip Country si se creeaza un obiect de tip GetCountryDto
            var resultDto = mapper.Map<GetCountryDto>(country);

            cache.Remove($"{CountryListCacheName}");
            
            return Result<GetCountryDto>.Success(resultDto);
        }
        catch (Exception)
        {
            return Result<GetCountryDto>.Failure();
        }

    }

    public async Task<Result> UpdateCountryAsync(int id, UpdateCountryDto updateDto)
    {
        try
        {
            if (id != updateDto.Id)
            {
                return Result.BadRequest(new Error(ErrorCodes.Validation, "Id route value doesn't match payload Id"));
            }
            var country = await context.Countries.FindAsync(id) ?? throw new KeyNotFoundException("Country not found");
            if (country is null)
            {
                return Result.NotFound(new Error(ErrorCodes.NotFound, $"Country '{id}' was not found"));
            }

            var duplicateName = await CountryExistsAsync(updateDto.Name);
            if (duplicateName)
            {
                return Result.Failure(new Error(ErrorCodes.Conflict, $"Country with the name '{updateDto.Name}' already exists."));
            }
            mapper.Map(updateDto, country);
            
            context.Countries.Update(country);
            await context.SaveChangesAsync();

            InvalidateCountryCache(id);

            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure();
        }
    }


    public async Task<Result> DeleteCountryAsync(int id)
    {
        try
        {
            var country = await context.Countries.FindAsync(id) ?? throw new KeyNotFoundException("Country not found");
            if (country is null)
            {
                return Result.NotFound(new Error(ErrorCodes.NotFound, $"Country '{id}' was not found"));
            }

            context.Countries.Remove(country);
            await context.SaveChangesAsync();
            InvalidateCountryCache(id);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure();
        }

    }

    private void InvalidateCountryCache(int id)
    {
        cache.Remove($"{CountrySingleCacheName}{id}");
    }

    public async Task<bool> CountryExistsAsync(int id)
    {
        return await context.Countries.AnyAsync(e => e.CountryId == id);
    }
    public async Task<bool> CountryExistsAsync(string name)
    {
        return await context.Countries.AnyAsync(e => e.Name.ToLower().Trim() == name.ToLower().Trim());
    }
}

