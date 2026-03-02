using System;

namespace ecommerce.Admin.Services.Dtos.LogDto;

public class LogDto
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
    public string Exception { get; set; }
    public string RequestPath { get; set; }
    public string ConnectionId { get; set; }
    public string SourceContext { get; set; }
    public string ActionName { get; set; }
    public string ClientIp { get; set; }
    public string Username { get; set; }
    public string UserId { get; set; }
    public string Application { get; set; }
    public string PageUrl { get; set; }
    public string MethodName { get; set; }
}
