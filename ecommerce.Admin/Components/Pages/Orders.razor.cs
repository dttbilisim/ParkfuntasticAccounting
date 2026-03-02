using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Admin.Components.Pages.Modals;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Linq.Dynamic.Core;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Orders
    {
        #region Injections
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

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
        protected IOrderService OrderService { get; set; }
        [Inject]
        protected ecommerce.Odaksodt.Abstract.IOdaksoftInvoiceService OdaksoftInvoiceService { get; set; }
        [Inject]
        protected IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject]
        protected IHostEnvironment _hostEnvironment { get; set; }
        #endregion

        // e-Fatura PDF indirme durumu
        protected bool _isDownloadingEInvoice = false;

        int count;
        protected List<OrderListDto> orders = null;
        protected RadzenDataGrid<OrderListDto>? radzenDataGrid = new();
        protected List<KeyValuePair<string, int>> orderStatus = new();
        protected List<KeyValuePair<string, int>> platformTypes = new();
        protected int SelectFilterStatus { get; set; }
        protected int? SelectFilterPlatformType { get; set; }
        protected DateTime? SelectFilterStartDate { get; set; }
        protected DateTime? SelectFilterEndDate { get; set; }
        protected string TopFilterTest { get; set; }
        private PageSetting pager;
        
        [Parameter] [SupplyParameterFromQuery] public int? status { get; set; }


        private async Task TopFilterChange()
        {
            var strTopFilter = GenerateTopFilter();

            // Clear
            if (string.IsNullOrEmpty(strTopFilter))
            {
                await radzenDataGrid.FirstPage(true);
            }
            else
            {

                pager = new PageSetting(strTopFilter, "", 0, strTopFilter != "" ? int.MaxValue : 25, false);

                var response = await OrderService.GetOrders(pager);
                if (response.Ok && response.Result != null && response.Result.Data != null)
                {
                    orders = response.Result.Data.ToList();
                    count = response.Result.DataCount;
                }
                else
                {
                    orders = new List<OrderListDto>();
                    count = 0;
                    if (!response.Ok)
                    {
                        NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    }
                }
                StateHasChanged();
            }
        }

        private string GenerateTopFilter()
        {
            var strTopFilter = !string.IsNullOrEmpty(TopFilterTest) ? $"(OrderNumber == null ? \"\" : OrderNumber).ToLower().Contains(\"{TopFilterTest}\".ToLower()) or (((Company.AccountName)) == null ? \"\" : ((Company.AccountName))).ToLower().Contains(\"{TopFilterTest}\".ToLower()) and " : "";
            strTopFilter += SelectFilterStatus > 0 ? $" OrderStatusType = {SelectFilterStatus} and " : "";
            strTopFilter += SelectFilterPlatformType.HasValue ? $" PlatformType == {SelectFilterPlatformType.Value} and " : "";

            strTopFilter += SelectFilterStartDate != null ? $" ShipmentDate >= \"{SelectFilterStartDate.Value.ToString("yyyy-MM-dd")}\" and " : "";

            strTopFilter += SelectFilterEndDate != null ? $" ShipmentDate <= \"{SelectFilterEndDate.Value.ToString("yyyy-MM-dd")}\" and " : "";


            if (!string.IsNullOrEmpty(strTopFilter))
            {
                strTopFilter = strTopFilter.Substring(0, strTopFilter.Length - 4).Trim();
            }

            return strTopFilter;
        }

        public async Task ExcelExportClick()
        {
            var excelFileUrl = string.Empty;

            var strTopFilter = !string.IsNullOrEmpty(TopFilterTest) ? $"(OrderNumber == null ? \"\" : OrderNumber).ToLower().Contains(\"{TopFilterTest}\".ToLower()) or (((Company.AccountName)) == null ? \"\" : ((Company.AccountName))).ToLower().Contains(\"{TopFilterTest}\".ToLower()) and " : "";
            strTopFilter += SelectFilterStatus > 0 ? $" OrderStatusType = {SelectFilterStatus} and " : "";
            strTopFilter += SelectFilterPlatformType.HasValue ? $" PlatformType == {SelectFilterPlatformType.Value} and " : "";

            strTopFilter += SelectFilterStartDate != null ? $" ShipmentDate >= \"{SelectFilterStartDate.Value.ToString("yyyy-MM-dd")}\" and " : "";

            strTopFilter += SelectFilterEndDate != null ? $" ShipmentDate <= \"{SelectFilterEndDate.Value.ToString("yyyy-MM-dd")}\" and " : "";


            if (!string.IsNullOrEmpty(strTopFilter))
            {
                strTopFilter = strTopFilter.Substring(0, strTopFilter.Length - 4).Trim();
            }
            pager = new PageSetting(strTopFilter, "", 0, int.MaxValue, false);
            var response = await OrderService.GetOrders(pager);


            try
            {

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.TabColor = System.Drawing.Color.Black;
                workSheet.DefaultRowHeight = 12;
                workSheet.Row(1).Height = 20;
                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "Sipariş No";
                workSheet.Cells[1, 2].Value = "Müşteri";
                workSheet.Cells[1, 3].Value = "Sipariş Durumu";
                workSheet.Cells[1, 4].Value = "Ödeme Tipi";
                workSheet.Cells[1, 5].Value = "Sipariş Tarihi";
                workSheet.Cells[1, 5].Value = "Toplam";
                var recordIndex = 2;
                foreach (var item in response.Result.Data.ToList())
                {
                    workSheet.Cells[recordIndex, 1].Value = item.OrderNumber;
                    workSheet.Cells[recordIndex, 2].Value = item.Company?.FullName ?? item.CustomerName ?? "";
                    workSheet.Cells[recordIndex, 3].Value = item.PaymentTypeId;
                    workSheet.Cells[recordIndex, 4].Value = item.ShipmentDate?.ToShortDateString();
                    workSheet.Cells[recordIndex, 5].Value = item.GrandTotal;
                    recordIndex++;
                }
                workSheet.Column(1).AutoFit();
                workSheet.Column(2).AutoFit();
                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                var fileBytes = await excel.GetAsByteArrayAsync();
                using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
                await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"Siparisler.xlsx", streamRef);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }

        }

        //private async Task TopFilterStatusChange()
        //{
        //    var strTopFilter = SelectFilterStatus > 0 ? $"OrderStatusType = {SelectFilterStatus}" : "";
        //    pager = new PageSetting(strTopFilter, "", 0, strTopFilter != "" ? int.MaxValue : 25, false);

        //    var response = await OrderService.GetOrders(pager);
        //    if (response.Ok && response.Result != null)
        //    {
        //        orders = response.Result.Data.ToList();
        //        count = response.Result.DataCount;
        //    }
        //    else
        //    {
        //        NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        //    }
        //    StateHasChanged();

        //}

        protected override async Task OnParametersSetAsync()
        {
            // Read status query parameter and set filter
            if (status.HasValue && status.Value > 0)
            {
                SelectFilterStatus = status.Value;
                // Trigger filter change after a short delay to ensure UI is ready
                await Task.Delay(100);
                await TopFilterChange();
            }
            await base.OnParametersSetAsync();
        }

        private async Task LoadData(LoadDataArgs args)
        {
            orderStatus = await OrderService.OrderStatus();
            
            // Initialize PlatformType dropdown data
            if (platformTypes == null || !platformTypes.Any())
            {
                platformTypes = new List<KeyValuePair<string, int>>
                {
                    new KeyValuePair<string, int>("Pazaryeri", (int)OrderPlatformType.Marketplace),
                    new KeyValuePair<string, int>("B2B", (int)OrderPlatformType.B2B)
                };
            }

            if (!string.IsNullOrEmpty(args.OrderBy)) args.OrderBy = args.OrderBy.Replace("np", "");
            if (!string.IsNullOrEmpty(args.Filter)) args.Filter = args.Filter.Replace("np", "");

            var strTopFilter = GenerateTopFilter();

            // top filter boş ise
            if (string.IsNullOrEmpty(args.Filter) && !string.IsNullOrEmpty(strTopFilter))
            {
                args.Filter = strTopFilter;
            }

            // top filter dolu, arg.Filter boş ise
            if (!string.IsNullOrEmpty(args.Filter) && !string.IsNullOrEmpty(strTopFilter))
            {
                args.Filter = (args.Filter + " and " + strTopFilter).Trim();
            }


            pager = new PageSetting(args.Filter ?? string.Empty, args.OrderBy ?? string.Empty, args.Skip, args.Top);
            var response = await OrderService.GetOrders(pager);
            if (response.Ok && response.Result != null && response.Result.Data != null)
            {
                orders = response.Result.Data.ToList();
                
                // MANUAL FIX: Ensure CustomerName is set
                if (orders != null)
                {
                    foreach (var order in orders)
                    {
                        if (string.IsNullOrWhiteSpace(order.CustomerName) && order.Company != null)
                        {
                            order.CustomerName = (order.Company.FirstName + " " + order.Company.LastName).Trim();
                        }
                        // Note: In Admin context, Company will be null, CustomerName should be set via helper properties
                    }
                }
                
                count = response.Result.DataCount;
            }
            else
            {
                // Fallback: Initialize empty list if data is null
                orders = new List<OrderListDto>();
                count = 0;
                
                if (!response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            StateHasChanged();
        }

        protected async Task OpenDetailForOrder(MouseEventArgs args, OrderListDto order)
        {
            await OpenDetailForOrder(order);
        }

        protected async Task OpenDetailForOrder(OrderListDto order)
        {
            await DialogService.OpenAsync<OrderDetail>("Sipariş Bilgileri", new Dictionary<string, object> { { "Id", order.Id } }, new DialogOptions() { Width = "1200px" });
            await radzenDataGrid.Reload();
        }

        /// <summary>
        /// e-Fatura kesilmiş satırları hafif yeşil arka plan ile gösterir
        /// </summary>
        protected void RowRender(RowRenderEventArgs<OrderListDto> args)
        {
            var hasEInvoice = args.Data.LinkedInvoices != null && args.Data.LinkedInvoices.Any(li => !string.IsNullOrEmpty(li.Ettn));
            if (hasEInvoice)
            {
                args.Attributes.Add("style", "background-color: #f0fdf4;");
            }
        }

        /// <summary>
        /// Sipariş listesinden e-Fatura PDF indirme
        /// </summary>
        protected async Task DownloadEInvoicePdf(OrderListDto order)
        {
            if (_isDownloadingEInvoice) return;

            var linkedInvoice = order.LinkedInvoices?.FirstOrDefault(li => !string.IsNullOrEmpty(li.Ettn));
            if (linkedInvoice == null) return;

            try
            {
                _isDownloadingEInvoice = true;
                StateHasChanged();

                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(linkedInvoice.Ettn);

                if (response != null && response.Status)
                {
                    if (!string.IsNullOrEmpty(response.Html))
                    {
                        await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { response.Html });
                    }
                    else if (!string.IsNullOrEmpty(response.ByteArray))
                    {
                        var fileName = $"e-Fatura_{linkedInvoice.InvoiceNo}_{linkedInvoice.Ettn}.pdf";
                        await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", new object?[] { response.ByteArray, fileName, "application/pdf" });
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Warning, "PDF İndirilemedi", "Odaksoft'tan fatura verisi alınamadı.");
                    }
                }
                else
                {
                    var errorMsg = response?.ExceptionMessage ?? "Bilinmeyen hata";
                    NotificationService.Notify(NotificationSeverity.Warning, "PDF İndirilemedi", errorMsg);
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "e-Fatura indirme hatası", ex.Message);
            }
            finally
            {
                _isDownloadingEInvoice = false;
                StateHasChanged();
            }
        }
    }
}

