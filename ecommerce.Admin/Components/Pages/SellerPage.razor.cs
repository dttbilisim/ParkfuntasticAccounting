using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.SellerDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class SellerPage
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected DialogService DialogService { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] public ISellerService SellerService { get; set; } = default!;
        [Inject] protected AuthenticationService Security { get; set; } = default!;

        protected List<SellerListDto>? sellers;
        protected RadzenDataGrid<SellerListDto>? radzenDataGrid = new();
        protected RadzenDataFilter<SellerListDto>? dataFilter;
        private readonly DialogOptions _dialogOptions = new() { Width = "700px" };
        private PageSetting _pager = default!;
        private int count;

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertSellerModal>("Satıcı Ekle", null, _dialogOptions);
            await radzenDataGrid!.Reload();
        }

        protected async Task EditRow(SellerListDto args)
        {
            await DialogService.OpenAsync<UpsertSellerModal>("Satıcı Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, _dialogOptions);
            await radzenDataGrid!.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, SellerListDto seller)
        {
            var confirm = await DialogService.Confirm("Seçilen satıcı silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm == true)
            {
                var deleteResult = await SellerService.DeleteSeller(new AuditWrapDto<SellerDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new SellerDeleteDto { Id = seller.Id }
                });
                if (deleteResult.Ok)
                {
                    await radzenDataGrid!.Reload();
                }
                else if (deleteResult.Exception != null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, deleteResult.Exception.Message);
                }
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderFilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderFilter = string.IsNullOrWhiteSpace(orderFilter) ? "Id desc" : orderFilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            _pager = new PageSetting(filter, orderFilter, args.Skip, args.Top);
            var response = await SellerService.GetSellers(_pager);
            if (response.Ok && response.Result?.Data != null)
            {
                sellers = response.Result.Data.OrderByDescending(x => x.Id).ToList();
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

