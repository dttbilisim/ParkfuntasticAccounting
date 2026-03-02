using ecommerce.Admin.Domain.Dtos.UserDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertUserModal
    {
        [Inject] public IUserService UserService { get; set; }
        [Inject] public AuthenticationService Security { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Parameter] public int? Id { get; set; }

        protected UserUpsertDto model = new();

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var res = await UserService.GetUserById(Id.Value);
                if (res.Ok)
                {
                    model = res.Result;
                }
            }
        }

        protected async Task Save(UserUpsertDto args)
        {
            var res = await UserService.UpsertUser(new AuditWrapDto<UserUpsertDto> { UserId = Security.User.Id, Dto = args });
            if (res.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Kayıt başarılı");
                await Close();
            }
            else if (res.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, res.Exception.Message);
            }
        }

        protected async Task Close()
        {
            await InvokeAsync(StateHasChanged);
            DialogService.Close(null);
        }
    }
}


