using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.DiscountDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Resources;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Discounts
    {
        #region Injection

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        public IDiscountService DiscountService { get; set; }

        [Inject]
        private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

        #endregion

        private Paging<List<DiscountListDto>> DiscountData { get; set; } = new();

        private RadzenDataGrid<DiscountListDto> DataGrid { get; set; }

        private void OnRadzenGridRender<TItem>(DataGridRenderEventArgs<TItem> args)
        {
            if (!args.FirstRender)
            {
                return;
            }

            _ = SetRadzenTexts(args.Grid);
        }

        private async Task SetRadzenTexts(RadzenComponent radzenComponent)
        {
            var parameters = ParameterView.FromDictionary(
                RadzenLocalizer.GetAllStrings().ToDictionary(l => l.Name, l => (object?) l.Value)
            );

            await radzenComponent.SetParametersAsync(parameters);

            await InvokeAsync(StateHasChanged);
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertDiscount>(
                "İndirim Ekle",
                options: new DialogOptions
                {
                    Width = "1200px",
                    CssClass = "mw-100"
                }
            );

            await DataGrid.Reload();
        }

        protected async Task EditRow(DiscountListDto args)
        {
            await DialogService.OpenAsync<UpsertDiscount>(
                "İndirim Düzenle",
                new Dictionary<string, object>
                {
                    { "Id", args.Id }
                },
                new DialogOptions
                {
                    Width = "1200px",
                    CssClass = "mw-100"
                }
            );

            await DataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(DiscountListDto dto)
        {
            if (await DialogService.Confirm(
                    "Seçilen indirimi silmek istediğinize emin misiniz?",
                    "Kayıt Sil",
                    new ConfirmOptions()
                    {
                        OkButtonText = "Evet",
                        CancelButtonText = "Hayır"
                    }
                ) == true)
            {
                var deleteResult = await DiscountService.DeleteDiscount(new DiscountDeleteDto() { Id = dto.Id });

                if (deleteResult.Ok)
                {
                    await DataGrid.Reload();
                }
                else
                {
                    await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                }
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            var pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);

            var response = await DiscountService.GetDiscounts(pager);

            if (response.Ok)
            {
                DiscountData = response.Result;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }
    }
}