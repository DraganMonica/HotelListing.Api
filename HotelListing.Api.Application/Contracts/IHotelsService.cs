using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;


namespace HotelListing.Api.Application.Contracts
{
    public interface IHotelsService
    {
        Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto);
        Task<Result> DeleteHotelAsync(int id);
        Task<Result<GetHotelDto>> GetHotelAsync(int id);
        Task<Result<PageResult<GetHotelDto>>> GetHotelsAsync(PaginationParameters paginationParameters, HotelFilterParameters filters);
        Task<bool> HotelExistsAsync(int id);
        Task<bool> HotelExistsAsync(string name);
        Task<Result> UpdateHotelAsync(int id, UpdateHotelDto hotelDto);
        
    }
}