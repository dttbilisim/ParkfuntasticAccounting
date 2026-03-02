using ecommerce.Core.Identity;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ecommerce.Domain.Shared.Middleware;

public class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, CurrentUser currentUser, ApplicationDbContext dbContext)
    {
        var user = context.User;
        
        // If user is authenticated, add UserBranch-based claims if they don't exist
        if (user?.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) && userId > 0)
            {
                // Check if ActiveBranchId claim already exists
                var existingBranchClaim = user.FindFirst("ActiveBranchId");
                if (existingBranchClaim == null)
                {
                    // Load UserBranch and Branch from database
                    try
                    {
                        var userBranch = await dbContext.UserBranches
                            .Where(ub => ub.UserId == userId)
                            .Select(ub => new { ub.BranchId, ub.Branch.CorporationId })
                            .FirstOrDefaultAsync();
                        
                        if (userBranch != null)
                        {
                            // Create new ClaimsIdentity with original claims + new claims
                            var identity = new ClaimsIdentity(user.Identity);
                            identity.AddClaim(new Claim("ActiveBranchId", userBranch.BranchId.ToString()));
                            identity.AddClaim(new Claim("ActiveCorporationId", userBranch.CorporationId.ToString()));
                            
                            // Plasiyer veya CustomerB2B için ek bilgiler
                            var appUser = await dbContext.AspNetUsers
                                .Where(u => u.Id == userId)
                                .Select(u => new { u.SalesPersonId, u.CustomerId })
                                .FirstOrDefaultAsync();
                            
                            if (appUser != null)
                            {
                                if (appUser.SalesPersonId.HasValue)
                                {
                                    identity.AddClaim(new Claim("SalesPersonId", appUser.SalesPersonId.Value.ToString()));
                                }
                                
                                // Plasiyer için: Header'dan gelen CustomerId'yi kullan (öncelikli)
                                // Eğer header'da CustomerId varsa, müşterinin ApplicationUser'ını bul ve impersonate et
                                if (context.Request.Headers.TryGetValue("X-Customer-Id", out var customerIdHeader) 
                                    && int.TryParse(customerIdHeader.FirstOrDefault(), out var headerCustomerId) 
                                    && headerCustomerId > 0)
                                {
                                    identity.AddClaim(new Claim("CustomerId", headerCustomerId.ToString()));
                                    
                                    // Müşterinin ApplicationUser'ını bul ve NameIdentifier'ı değiştir (impersonation)
                                    // Admin Blazor tarafındaki SetSelectedCustomer ile aynı mantık
                                    var customerUser = await dbContext.AspNetUsers
                                        .Where(u => u.CustomerId == headerCustomerId)
                                        .Select(u => new { u.Id })
                                        .FirstOrDefaultAsync();
                                    
                                    if (customerUser != null)
                                    {
                                        // Plasiyerin orijinal ID'sini sakla
                                        identity.AddClaim(new Claim("OriginalUserId", userId.ToString()));
                                        
                                        // NameIdentifier'ı müşterinin userId'sine değiştir
                                        var existingNameId = identity.FindFirst(ClaimTypes.NameIdentifier);
                                        if (existingNameId != null)
                                        {
                                            identity.RemoveClaim(existingNameId);
                                        }
                                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, customerUser.Id.ToString()));
                                        
                                        Console.WriteLine($"[CurrentUserMiddleware] Plasiyer impersonation: UserId {userId} -> {customerUser.Id} (CustomerId: {headerCustomerId})");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[CurrentUserMiddleware] Using CustomerId from header: {headerCustomerId} (no ApplicationUser found)");
                                    }
                                }
                                else if (appUser.CustomerId.HasValue)
                                {
                                    // Header'da yoksa, kullanıcının kendi CustomerId'sini kullan
                                    identity.AddClaim(new Claim("CustomerId", appUser.CustomerId.Value.ToString()));
                                }
                            }
                            
                            // Create new ClaimsPrincipal and replace the HttpContext.User
                            user = new ClaimsPrincipal(identity);
                            context.User = user;
                            
                            Console.WriteLine($"[CurrentUserMiddleware] Added claims for UserId {userId}: BranchId={userBranch.BranchId}, CorporationId={userBranch.CorporationId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request
                        Console.WriteLine($"[CurrentUserMiddleware] Error loading UserBranch: {ex.Message}");
                    }
                }
            }
        }
        
        currentUser.SetUser(user);
    
        await _next(context);
      
    }
}