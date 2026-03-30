using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace HotelListing.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController(IHotelsService hotelsService) : BaseApiController
    {

        // GET: api/Hotels
        [HttpGet]
        public async Task<ActionResult<PageResult<GetHotelDto>>> GetHotels([FromQuery] PaginationParameters paginationParameters, [FromQuery] HotelFilterParameters filters)
        {
            var result = await hotelsService.GetHotelsAsync(paginationParameters, filters);
            return ToActionResult(result);
        }

        // GET: api/Hotels/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GetHotelDto>> GetHotel(int id)
        {
            //
            var result = await hotelsService.GetHotelAsync(id);
            return ToActionResult(result);
        }

        // PUT: api/Hotels/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        [Authorize(Roles ="Administrator")]
        public async Task<IActionResult> PutHotel(int id, UpdateHotelDto hotelDto)
        {
            var result = await hotelsService.UpdateHotelAsync(id, hotelDto);
            return ToActionResult(result);
        }

        // POST: api/Hotels
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<Hotel>> PostHotel(CreateHotelDto hotelDto)
        {
            var result = await hotelsService.CreateHotelAsync(hotelDto);
            if (!result.IsSuccess)
                return MapErrorsToResponse(result.Errors);

            return CreatedAtAction(
                nameof(GetHotel), new { id = result.Value!.Id }, result.Value
            );
        }

        // DELETE: api/Hotels/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            //delete hotel
            var result = await hotelsService.DeleteHotelAsync(id);
            return ToActionResult(result);
        }

        



    }
}
