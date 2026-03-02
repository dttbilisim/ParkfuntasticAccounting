using System;

namespace ecommerce.Domain.Shared.Dtos;

public class OnlineUserDto
{
    public string UserId { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string IpAddress { get; set; }
    public string LastPageUrl { get; set; }
    public DateTime LastActiveTime { get; set; }
    public string ConnectionId { get; set; }
    public string Application { get; set; } // Web, Admin, Mobile
    
    // Plasiyer konum takibi için — sadece salesman rolünde dolu
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
