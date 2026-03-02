using Microsoft.AspNetCore.Http;

namespace ecommerce.Web.Domain.MiddleWares;

public class CookieUserCheckMiddleware(RequestDelegate next){
    public async Task InvokeAsync(HttpContext context)
    {
        var fullName = context.Request.Cookies["AutCookie"];

        if (!string.IsNullOrEmpty(fullName))
        {
            context.Items["AutCookie"] = fullName;
        }

        await next(context);
    }
}