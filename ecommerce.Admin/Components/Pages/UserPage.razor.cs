using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.UserDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class UserPage
    {
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] public IUserService UserService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }

        protected List<UserListDto> users = null;
        protected RadzenDataGrid<UserListDto>? radzenDataGrid = new();
        protected RadzenDataFilter<UserListDto>? dataFilter;
        private new DialogOptions DialogOptions = new() { Width = "900px" };
        private PageSetting pager;
        int count;

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertUserModal>("Kullanıcı Ekle", null, DialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(UserListDto args)
        {
            await DialogService.OpenAsync<UpsertUserModal>("Kullanıcı Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task AddRow(UserListDto args)
        {
            await DialogService.OpenAsync<UpsertUserModal>("Kullanıcı Ekle", new Dictionary<string, object> { { "Id", null } }, DialogOptions);
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, UserListDto user)
        {
            if (await DialogService.Confirm("Seçilen kullanıcı silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                    new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                var deleteResult = await UserService.DeleteUser(new Core.Helpers.AuditWrapDto<UserDeleteDto>()
                { UserId = Security.User.Id, Dto = new UserDeleteDto() { Id = user.Id } });
                if (deleteResult != null)
                {
                    await radzenDataGrid.Reload();
                }
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderfilter = orderfilter == string.Empty ? "Id desc" : orderfilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            pager = new PageSetting(filter, orderfilter, args.Skip, args.Top);
            var response = await UserService.GetUsers(pager);
            if (response.Ok && response.Result?.Data != null)
            {
                users = response.Result.Data.OrderByDescending(x => x.Id).ToList();
                count = response.Result.DataCount;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
    }
}


