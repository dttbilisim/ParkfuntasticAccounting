using Microsoft.AspNetCore.Components;
using Blazored.Modal.Services;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Core.Entities;
using Radzen;
using StackExchange.Redis;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Identity;
using System.Security.Claims;

namespace ecommerce.Web.Components.Layout;

public partial class MainLayout
{
    [CascadingParameter] public IModalService ModalService { get; set; }
    [Inject] private IUserCarService UserCarService { get; set; }
    [Inject] private DialogService DialogService { get; set; }
    [Inject] private NavigationManager Navigation { get; set; }
    [Inject] private IConnectionMultiplexer Redis { get; set; }
    [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        
        try
        {
            var user = HttpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                return;
            }

            var result = await UserCarService.GetAllUserCarsForCurrentUserAsync();
            var cars = result.Ok ? result.Result : new List<UserCars>();
            if (cars == null || cars.Count == 0)
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? user.FindFirst(ecommerceClaimTypes.UserId)?.Value;
                var cacheKey = string.IsNullOrWhiteSpace(userIdClaim) ? "prompt:user:anon:addcar" : $"prompt:user:{userIdClaim}:addcar";
                var db = Redis.GetDatabase();
                var suppressed = await db.StringGetAsync(cacheKey);
                if (suppressed.HasValue) return;

                bool? confirm = await DialogService.Confirm(
                    "Sistemde aracınıza uygun parçaları bulabilmek için lütfen aracınızı kaydedin.",
                    "Araç kaydı önerisi",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" }
                );
                if (confirm == true)
                {
                    try
                    {
                        var parameters = new Blazored.Modal.ModalParameters();
                        parameters.Add(nameof(Components.Modals.AddOrUpdateCarModel.EditableCars), new UserCars());
                        var options = new Blazored.Modal.ModalOptions
                        {
                            DisableBackgroundCancel = false,
                            HideHeader = true,
                            Size = Blazored.Modal.ModalSize.Large,
                            HideCloseButton = true,
                            AnimationType = Blazored.Modal.ModalAnimationType.FadeInOut
                        };
                        ModalService?.Show<Components.Modals.AddOrUpdateCarModel>("", parameters, options);
                    }
                    catch
                    {
                        Navigation.NavigateTo("/user-dashboard");
                    }
                }

                await db.StringSetAsync(cacheKey, "asked", TimeSpan.FromDays(7));
            }
        }
        catch
        {
        }
    }
}