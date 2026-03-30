namespace HotelListing.Api.Common.Models.Paging;

public class PageResult<T> 
{
    public IEnumerable<T> Data { get; set; } = [];
    public PaginationMetadata Metadata { get; set; } = new();
}
