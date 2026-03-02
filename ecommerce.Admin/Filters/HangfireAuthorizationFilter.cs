using Hangfire.Dashboard;

namespace ecommerce.Admin.Filters;

public class HangfireAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
    public Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        var result = httpContext.User.Identity?.IsAuthenticated == true
                     && (httpContext.User.IsInRole("SuperAdmin"));

        return Task.FromResult(result);
    }
}