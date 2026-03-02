using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class GlobalExceptionLog: AuditableEntity<int>
{
    public string Path { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public int LogType{get;set;}
}