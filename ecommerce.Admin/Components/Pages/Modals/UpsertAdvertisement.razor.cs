using ecommerce.Admin.ConfigureValidators;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.SellerDto;
using ecommerce.Admin.Domain.Dtos.SellerItemDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using System.Security.Claims;
using static ecommerce.Admin.ConfigureValidators.Validations;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertAdvertisement : ComponentBase
    {
        [Parameter] public int? Id { get; set; }

        [Inject] ISellerItemService SellerItemService { get; set; }
        [Inject] ISellerService SellerService { get; set; }
        [Inject] IProductService ProductService { get; set; }
        [Inject] NotificationService NotificationService { get; set; }
        [Inject] DialogService DialogService { get; set; }
        [Inject] AuthenticationStateProvider AuthenticationStateProvider { get; set; }

        private SellerItemUpsertDto model = new SellerItemUpsertDto();
        private List<SellerListDto> sellers = new List<SellerListDto>();
        private List<ProductListDto> products = new List<ProductListDto>();
        private int productsCount;
        public List<string> ValidationErrors = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadSellers();
            if (Id.HasValue)
            {
                var result = await SellerItemService.GetSellerItemById(Id.Value);
                if (result.Ok)
                {
                    model = result.Result;
                    // Pre-load the selected product so it shows up in the dropdown
                    var productResult = await ProductService.GetProducts(new List<int> { model.ProductId });
                    if (productResult.Ok && productResult.Result != null)
                    {
                        products = productResult.Result;
                        productsCount = products.Count;
                    }
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.GetMetadataMessages()
                    });
                    DialogService.Close();
                }
            }
            else
            {
                model.Status = 1; // Default Active
                model.Currency = "TRY";
                model.Unit = "Adet";
            }
        }

        private async Task LoadSellers()
        {
            var pager = new PageSetting { Take = 1000, Skip = 0 }; // Fetch enough sellers
            var result = await SellerService.GetSellers(pager);
            if (result.Ok)
            {
                sellers = result.Result.Data;
            }
        }

        private async Task LoadProducts(LoadDataArgs args)
        {
            var result = await ProductService.SearchProducts(args.Filter);
            if (result.Ok)
            {
                products = result.Result;
                productsCount = products.Count; // Search limit is 50 usually
            }
        }

        private async Task OnSubmit(SellerItemUpsertDto args)
        {
             // Fluent Validation Check
             var validator = new SellerItemUpsertDtoValidator();
             var validationResult = validator.Validate(model);
             if (!validationResult.IsValid)
             {
                 ValidationErrors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
                 await ShowErrors();
                 return;
             }

             var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
             var user = authState.User;
             var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             int.TryParse(userIdStr, out int userId);

             if (userId == 0)
             {
                 NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = "Kullanıcı oturumu bulunamadı." });
                 return;
             }

             if (Id.HasValue)
             {
                 var result = await SellerItemService.UpdateSellerItem(args, userId);
                 if (result.Ok)
                 {
                     NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Başarılı", Detail = "İlan güncellendi." });
                     DialogService.Close(true);
                 }
                 else
                 {
                     NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = result.GetMetadataMessages() });
                 }
             }
             else
             {
                 var result = await SellerItemService.AddSellerItem(args, userId);
                 if (result.Ok)
                 {
                     NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Başarılı", Detail = "İlan eklendi." });
                     DialogService.Close(true);
                 }
                 else
                 {
                     NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = result.GetMetadataMessages() });
                 }
             }
        }

        protected async Task ShowErrors()
        {
            List<Dictionary<string, string>> error = await PrepareErrorsForWarningModal(ValidationErrors);
            Dictionary<string, object> param = new();
            param.Add("Errors", error);
            await DialogService.OpenAsync<ValidationModal>("Uyarı", param);
            ValidationErrors.Clear();
        }

        private async Task<List<Dictionary<string, string>>> PrepareErrorsForWarningModal(List<string> errors)
        {
            List<Dictionary<string, string>> error = new();
            foreach (var errorText in errors)
            {
                Dictionary<string, string> messageDictionary = new Dictionary<string, string>();
                var parts = errorText.Split("-");
                if (parts.Length > 1)
                {
                     messageDictionary.Add(parts[0], parts[1]);
                }
                else
                {
                     messageDictionary.Add("Hata", errorText);
                }
                error.Add(messageDictionary);
            }
            return error;
        }

        private void Cancel()
        {
            DialogService.Close(false);
        }
    }
}
