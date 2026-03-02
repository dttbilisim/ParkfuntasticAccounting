using System.Collections.Generic;
using System.Threading.Tasks;
using ecommerce.Admin.Services.Dtos.LogDto;

namespace ecommerce.Admin.Services.Interfaces;

public interface ILogService
{
    Task<(List<LogDto> Logs, long Total)> GetLogsAsync(int page, int pageSize, string? level = null, string? search = null, string? application = null);
}
