using HotelListing.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HotelsController : ControllerBase
{
    private static List<Hotel> hotels = new List<Hotel>
    {
        new Hotel { Id = 1, Name = "Hotel A", Address = "123 Main St", Rating = 4.5 },
        new Hotel { Id = 2, Name = "Hotel B", Address = "456 Elm St", Rating = 3.8 },
        new Hotel { Id = 3, Name = "Hotel C", Address = "789 Oak St", Rating = 4.2 }
    };
    // GET: api/<HotelsController>
    [HttpGet]
    public ActionResult<IEnumerable<Hotel>> Get()
    {
        return Ok(hotels);
    }

    // GET api/<HotelsController>/5
    [HttpGet("{id}")]
    public ActionResult<Hotel> Get(int id)
    {
        var hotel=hotels.FirstOrDefault(h => h.Id == id);
        if(hotel == null)
        {
            return NotFound();
        }
        return Ok(hotel);
    }

    // POST api/<HotelsController>
    [HttpPost]
    public ActionResult<Hotel> Post([FromBody] Hotel newHotel)
    {
        if(hotels.Any(h => h.Id == newHotel.Id))
        {
            return BadRequest("Hotel with the same Id already exists.");
        }
        hotels.Add(newHotel);
        return CreatedAtAction(nameof(Get), new { id = newHotel.Id }, newHotel);
    }

    // PUT api/<HotelsController>/5
    [HttpPut("{id}")]
    public ActionResult Put(int id, [FromBody] Hotel updatedHotel)
    {
        var existingHotel=hotels.FirstOrDefault(h => h.Id == id);
        if (existingHotel == null)
        {
            return NotFound();
        }
        existingHotel.Name = updatedHotel.Name;
        existingHotel.Address = updatedHotel.Address;
        existingHotel.Rating = updatedHotel.Rating;

        return NoContent();
    }

    // DELETE api/<HotelsController>/5
    [HttpDelete("{id}")]
    public ActionResult Delete(int id)
    {
        var hotel = hotels.FirstOrDefault(h => h.Id == id);
        if (hotel == null)
        {
            return NotFound(new {message="Hotel not found"});
        }
        hotels.Remove(hotel);
        return NoContent();
    }
}
