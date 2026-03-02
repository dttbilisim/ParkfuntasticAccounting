using Blazored.FluentValidation;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.UnitDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

using ecommerce.Admin.CustomComponents.Modals;
using static ecommerce.Admin.ConfigureValidators.Validations;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductUnit
    {
        #region Injection
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IProductUnitService ProductUnitService { get; set; } = null!;
        [Inject] public IProductService ProductService { get; set; } = null!;
        [Inject] public IUnitService UnitService { get; set; } = null!;
        #endregion

        [Parameter] public int? Id { get; set; }
        [Parameter] public int? ProductId { get; set; }

        public List<string> ValidationErrors = new();
        protected ProductUnitUpsertDto? productUnit;
        protected bool Saving { get; set; }
        protected IEnumerable<ProductListDto> Products { get; set; } = new List<ProductListDto>();
        protected IEnumerable<UnitListDto> Units { get; set; } = new List<UnitListDto>();
        protected FluentValidationValidator? _fluentValidationValidator;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();

            if (Id.HasValue)
            {
                var response = await ProductUnitService.GetProductUnitById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    productUnit = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    productUnit = new ProductUnitUpsertDto { UnitValue = 1 };
                }
            }
            else
            {
                productUnit = new ProductUnitUpsertDto { UnitValue = 1 };
                if (ProductId.HasValue)
                {
                    productUnit.ProductId = ProductId.Value;
                }
            }
        }

        private async Task LoadData()
        {
            // Ürünler
            var productRs = await ProductService.GetProducts(new PageSetting
            {
                Filter = string.Empty,
                OrderBy = "Id desc",
                Skip = 0,
                Take = 500
            });

            if (productRs.Ok && productRs.Result?.Data != null)
            {
                Products = productRs.Result.Data;
            }

            // Birimler
            var unitRs = await UnitService.GetUnits();
            if (unitRs.Ok && unitRs.Result != null)
            {
                Units = unitRs.Result;
            }

            await Task.CompletedTask;
        }

        protected async Task FormSubmit(ProductUnitUpsertDto args)
        {
            try
            {
                Saving = true;

                var response = await ProductUnitService.UpsertProductUnit(new AuditWrapDto<ProductUnitUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = args
                });

                if (response.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = Id.HasValue ? "Ürün birimi başarıyla güncellendi." : "Ürün birimi başarıyla eklendi."
                    });
                    DialogService.Close(args);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }

        protected async Task ShowErrors()
        {
            if (productUnit == null) return;

            var validator = new ProductUnitUpsertValidator();
            var res = validator.Validate(productUnit);

            ValidationErrors.Clear();
            ValidationErrors.AddRange(res.Errors.Select(x => x.ErrorMessage));
            
            List<Dictionary<string, string>> error = await PrepareErrorsForWarningModal(ValidationErrors);

            Dictionary<string, object> param = new();
            param.Add("Errors", error);
            await DialogService.OpenAsync<ValidationModal>("Uyari", param);

            ValidationErrors.Clear();
        }

        private async Task<List<Dictionary<string, string>>> PrepareErrorsForWarningModal(List<string> errors)
        {
            List<Dictionary<string, string>> error = new();
            foreach (var errorText in errors)
            {
                if (errorText.Contains("-"))
                {
                    Dictionary<string, string> messageDictionary = new Dictionary<string, string>();
                    messageDictionary.Add(errorText.Split("-")[0], errorText.Split("-")[1]);
                    error.Add(messageDictionary);
                }
                else
                {
                    Dictionary<string, string> messageDictionary = new Dictionary<string, string>();
                    messageDictionary.Add("Hata", errorText);
                    error.Add(messageDictionary);
                }
            }
            return error;
        }
    }
}
