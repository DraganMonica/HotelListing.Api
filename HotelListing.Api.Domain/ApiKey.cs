namespace HotelListing.Api.Domain
{
    public class ApiKey
    {
        public int Id { get; set; }
        public string Key{ get; set; }=string.Empty;
        public string AppName { get; set; } = string.Empty;
        public DateTimeOffset? ExpiresAtUtc { get; set; }//data+ora+timezone
        public DateTimeOffset CreatedAtUtc { get; set; }=DateTimeOffset.UtcNow;
        public bool IsActive => !ExpiresAtUtc.HasValue || ExpiresAtUtc.Value>DateTimeOffset.UtcNow;
    }
}
