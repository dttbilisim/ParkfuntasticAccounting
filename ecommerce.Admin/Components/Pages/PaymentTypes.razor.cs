using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class PaymentTypes
    {
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IPaymentTypeService PaymentTypeService { get; set; } = null!;

        protected RadzenDataGrid<PaymentTypeListDto>? grid;
        protected IEnumerable<PaymentTypeListDto>? paymentTypes;
        protected int count;
        protected bool isLoading;

        protected async Task LoadData(LoadDataArgs args)
        {
            isLoading = true;
            try
            {
                var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
                args.Filter = args.Filter.Replace("np", "");
                var pager = new PageSetting
                {
                    Skip = args.Skip,
                    Take = args.Top,
                    Filter = args.Filter,
                    OrderBy = orderfilter
                };

                var response = await PaymentTypeService.GetPaymentTypes(pager);
                if (response.Ok && response.Result != null)
                {
                    paymentTypes = response.Result.Data;
                    count = response.Result.DataCount;
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        protected async Task AddButtonClick()
        {
            var result = await DialogService.OpenAsync<Modals.UpsertPaymentType>("Yeni Ödeme Tipi",
                options: new DialogOptions { Width = "600px", Height = "auto", ShowTitle = false, ShowClose = false });

            if (result == true)
            {
                await grid!.Reload();
            }
        }

        protected async Task EditRow(PaymentTypeListDto item)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertPaymentType>("Ödeme Tipi Düzenle",
                new Dictionary<string, object> { { "Id", item.Id } },
                options: new DialogOptions { Width = "600px", Height = "auto", ShowTitle = false, ShowClose = false });

            if (result == true)
            {
                await grid!.Reload();
            }
        }

        protected async Task DeleteRow(PaymentTypeListDto item)
        {
            var confirm = await DialogService.Confirm($"{item.Name} ödeme tipini silmek istediğinize emin misiniz?", "Silme Onayı",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

            if (confirm == true)
            {
                var response = await PaymentTypeService.DeletePaymentType(new AuditWrapDto<PaymentTypeDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new PaymentTypeDeleteDto { Id = item.Id }
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Ödeme tipi silindi.");
                    await grid!.Reload();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                }
            }
        }
    }
}
