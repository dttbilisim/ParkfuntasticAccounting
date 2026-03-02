using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Services.Dtos.LogDto;
using ecommerce.Admin.Services.Interfaces;
using Nest;
using Newtonsoft.Json.Linq;

namespace ecommerce.Admin.Services.Concreate;

public class LogService : ILogService
{
    private readonly IElasticClient _elasticClient;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "logs";

    public LogService(IElasticClient elasticClient, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _elasticClient = elasticClient;
        _permissionService = permissionService;
    }

    public async Task<(List<LogDto> Logs, long Total)> GetLogsAsync(int page, int pageSize, string? level = null, string? search = null, string? application = null)
    {
        var searchDescriptor = new SearchDescriptor<dynamic>()
            .Index("ecommerce-logs-*")
            .From(page * pageSize)
            .Size(pageSize)
            .Sort(ss => ss.Descending("@timestamp"));

        var mustQueries = new List<QueryContainer>();

        if (!string.IsNullOrEmpty(level) && level != "All")
        {
            mustQueries.Add(new MatchQuery { Field = "level", Query = level });
        }

        if (!string.IsNullOrEmpty(application) && application != "All")
        {
            mustQueries.Add(new MatchQuery { Field = "fields.Application", Query = application });
        }

        if (!string.IsNullOrEmpty(search))
        {
            mustQueries.Add(new MultiMatchQuery
            {
                Fields = new[] { "message", "exception", "fields.RequestPath", "fields.SourceContext", "fields.Application" },
                Query = search
            });
        }

        if (mustQueries.Any())
        {
            searchDescriptor.Query(q => q.Bool(b => b.Must(mustQueries.ToArray())));
        }

        var response = await _elasticClient.SearchAsync<dynamic>(searchDescriptor);

        if (!response.IsValid)
        {
            // Elastic connection error or index not found
            return (new List<LogDto>(), 0);
        }

        var logs = new List<LogDto>();

        foreach (var hit in response.Hits)
        {
            // Nest uses Newtonsoft.Json by default internally unless configured otherwise for source serializer.
            // But we configured JsonNetSerializer.Default in ConfigureServices.
            // So Source is likely JObject or similar dynamic.
            
            var source = hit.Source as JObject;
            if (source == null) continue;

            var log = new LogDto
            {
                Timestamp = source["@timestamp"]?.ToObject<DateTime>() ?? DateTime.MinValue,
                Level = source["level"]?.ToString(),
                Message = source["message"]?.ToString() ?? source["messageTemplate"]?.ToString(),
                Exception = source["exception"]?.ToString(),
                ClientIp = source["fields"]?["ClientIp"]?.ToString() ?? source["ClientIp"]?.ToString(),
                Username = source["fields"]?["Username"]?.ToString() ?? source["Username"]?.ToString(),
                UserId = source["fields"]?["UserId"]?.ToString() ?? source["UserId"]?.ToString(),
                Application = source["fields"]?["Application"]?.ToString() ?? "Unknown",
                PageUrl = source["fields"]?["PageUrl"]?.ToString() ?? source["PageUrl"]?.ToString(),
                MethodName = source["fields"]?["MethodName"]?.ToString() ?? source["MethodName"]?.ToString() ?? source["fields"]?["ActionName"]?.ToString()
            };

            if (source["fields"] is JObject fields)
            {
                log.RequestPath = fields["RequestPath"]?.ToString();
                log.ConnectionId = fields["ConnectionId"]?.ToString();
                log.SourceContext = fields["SourceContext"]?.ToString();
                log.ActionName = fields["ActionName"]?.ToString();
            }

            logs.Add(log);
        }

        return (logs, response.Total);
    }
}
