using ecommerce.Admin.Domain.Dtos.SellerItemDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;
using Radzen;
using ecommerce.Admin.Domain.Dtos.SellerDto;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Advertisements
    {
        [Inject] public ISellerItemService SellerItemService { get; set; } = null!;
        [Inject] public ISellerService SellerService { get; set; } = null!;

        protected RadzenDataGrid<SellerItemListDto> grid = null!;
        protected List<SellerItemListDto>? sellerItems;
        protected int count;
        protected bool isLoading = false;
        protected string searchText = "";
        protected int? selectedSellerId;
        protected List<SellerListDto> sellers = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadSellers();
        }

        private async Task LoadSellers()
        {
            var result = await SellerService.GetSellers(new PageSetting { Take = 100 });
            if (result.Ok && result.Result != null)
            {
                sellers = result.Result.Data;
            }
        }

        protected async Task LoadData(LoadDataArgs args)
        {
            isLoading = true;

            var pager = new PageSetting
            {
                Skip = args.Skip,
                Take = args.Top,
                Search = searchText
            };

            // Seller filter logic (if integrated in service search or extra param)
            // For now, we use search text if seller is selected or handle it in service
            if (selectedSellerId.HasValue)
            {
                // If service doesn't support SellerId directly, we could pass it in search or update service
                // Recommended: Update service to support explicit SellerId filter if needed
                // But for start, we rely on search if it matches seller name or we can extend PageSetting
            }

            var result = await SellerItemService.GetSellerItems(pager, selectedSellerId);

            if (result.Ok && result.Result != null)
            {
                sellerItems = result.Result.Data;
                count = result.Result.DataCount;
            }

            isLoading = false;
        }

        protected async Task OnSearch()
        {
            await grid.FirstPage();
            await grid.Reload();
        }

        protected async Task HandleKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter")
            {
                await OnSearch();
            }
        }

        protected async Task ClearSearch()
        {
            searchText = "";
            await OnSearch();
        }

        protected async Task OnSellerChange(object value)
        {
             await grid.FirstPage();
             await grid.Reload();
        }

        [Inject] public DialogService DialogService { get; set; } = null!;

        protected async Task OnRowClick(DataGridRowMouseEventArgs<SellerItemListDto> args)
        {
            await OpenUpsertModal(args.Data.Id);
        }

        protected async Task OpenUpsertModal(int? id)
        {
            await DialogService.OpenAsync<Modals.UpsertAdvertisement>(
                id.HasValue ? "İlan Düzenle" : "Yeni İlan Ekle",
                new Dictionary<string, object> { { "Id", id } },
                new DialogOptions { Width = "700px", Height = "auto", Resizable = true, Draggable = true }
            );

            await grid.Reload();
        }
    }
}
