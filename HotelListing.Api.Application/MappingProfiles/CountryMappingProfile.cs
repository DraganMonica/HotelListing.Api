using AutoMapper;
using HotelListing.Api.Application.DTOs.Country;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Domain;

namespace HotelListing.Api.Application.MappingProfiles;

public class CountryMappingProfile : Profile
{
    public CountryMappingProfile()
    {
        // pt  
        CreateMap<Country, GetCountriesDto>()
                 .ForMember(d => d.Id, config => config.MapFrom(s => s.CountryId));
        CreateMap<Country, GetCountryDto>()
                 .ForMember(d => d.Id, config => config.MapFrom(s => s.CountryId));
        CreateMap<Hotel, GetHotelSlimDto>();
        CreateMap<CreateCountryDto, Country>();
        CreateMap<UpdateCountryDto, Country>();
    }
}
