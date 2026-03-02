using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Web.Domain.MiddleWares;


public class ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger){
    public async Task Invoke(HttpContext context, IServiceProvider serviceProvider){
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        try{
            await next(context);
        } catch(Exception ex){
            db.GlobalExceptionLogs.Add(new GlobalExceptionLog{
                    Path = context.Request.Path,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace!,
                    LogType = (int) LogType.Error,
                    CreatedId = 1,
                    CreatedDate = DateTime.UtcNow
                }
            );
            await db.SaveChangesAsync();
            logger.LogError(ex, "Global Hata Yakalandı");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Bir hata oluştu.");
        }
    }
}