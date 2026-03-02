using ecommerce.Admin.Domain.Dtos.PriceListDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class PriceLists
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected TooltipService TooltipService { get; set; } = null!;
        [Inject] protected ContextMenuService ContextMenuService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IPriceListService PriceListService { get; set; } = null!;
        #endregion

        int count;
        protected List<PriceListListDto>? priceLists;
        protected RadzenDataGrid<PriceListListDto>? radzenDataGrid = new();
        private PageSetting pager;

        // Master-Detail için seçilen fiyat listesi ve detay satırlar
        protected PriceListUpsertDto? selectedPriceList;
        protected List<PriceListItemUpsertDto> detailItems = new();

       
        protected async Task LoadData(LoadDataArgs args)
        {
            try
            {
                var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
                args.Filter = args.Filter.Replace("np", "");
                pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);

                var response = await PriceListService.GetPriceLists(pager);
                if (response.Ok && response.Result != null)
                {
                    priceLists = response.Result.Data?.ToList();
                    count = response.Result.DataCount;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected async Task AddButtonClick()
        {
            await OpenUpsertDialog(null);
        }

        // Master grid'de satıra tıklayınca: hem detayı al hem modali aç
        protected async Task OnRowSelect(PriceListListDto priceList)
        {
            try
            {
                var rs = await PriceListService.GetPriceListById(priceList.Id);
                if (rs.Ok && rs.Result != null)
                {
                    selectedPriceList = rs.Result;
                    detailItems = rs.Result.Items ?? new List<PriceListItemUpsertDto>();

                    // Aynı anda düzenleme modali de açılsın
                    await OpenUpsertDialog(priceList.Id);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Detay yüklenirken hata: {ex.Message}");
            }
        }

        protected async Task EditRow(PriceListListDto priceList)
        {
            await OpenUpsertDialog(priceList.Id);
        }

        protected async Task OpenUpsertDialog(int? id = null)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertPriceList>(
                id.HasValue ? "Fiyat Listesi Düzenle" : "Yeni Fiyat Listesi",
                new Dictionary<string, object> { { "Id", id } },
                new DialogOptions
                {
                    Width = "1200px",
                    Height = "800px",
                    Resizable = true,
                    Draggable = true,
                    CloseDialogOnOverlayClick = false
                });

            if (result != null && radzenDataGrid != null)
            {
                await radzenDataGrid.Reload();
            }
        }

        protected async Task GridDeleteButtonClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs args, PriceListListDto priceList)
        {
            try
            {
                var confirm = await DialogService.Confirm(
                    $"{priceList.Name} fiyat listesini silmek istediğinizden emin misiniz?",
                    "Fiyat Listesi Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

                if (confirm == true)
                {
                    var response = await PriceListService.DeletePriceList(new AuditWrapDto<PriceListDeleteDto>
                    {
                        UserId = Security.User.Id,
                        Dto = new PriceListDeleteDto { Id = priceList.Id }
                    });

                    if (response.Ok)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Başarılı",
                            Detail = "Fiyat listesi silindi."
                        });

                        if (radzenDataGrid != null)
                        {
                            await radzenDataGrid.Reload();
                        }
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected void RowRender(RowRenderEventArgs<PriceListListDto> args)
        {
            // Custom row rendering if needed
        }
    }
}
