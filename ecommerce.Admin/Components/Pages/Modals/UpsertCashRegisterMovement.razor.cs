using ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto;
using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Dtos;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCashRegisterMovement
    {
        [Parameter] public int? Id { get; set; }

        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public ICashRegisterMovementService MovementService { get; set; } = null!;
        [Inject] public ICashRegisterService CashRegisterService { get; set; } = null!;
        [Inject] public ICustomerService CustomerService { get; set; } = null!;
        [Inject] public ISalesPersonService SalesPersonService { get; set; } = null!;
        [Inject] public IPaymentTypeService PaymentTypeService { get; set; } = null!;
        [Inject] public ICurrencyAdminService CurrencyService { get; set; } = null!;

        protected CashRegisterMovementUpsertDto? Model { get; set; }
        protected bool Saving { get; set; }

        protected List<CashRegisterListDto> CashRegisterList { get; set; } = new();
        protected List<CustomerListDto> CustomerList { get; set; } = new();
        protected List<SalesPersonListDto> SalesPersonList { get; set; } = new();
        protected List<SelectItemDto<int?>> PaymentTypeOptions { get; set; } = new();
        protected List<SelectItemDto<int?>> FilteredPaymentTypeOptions => GetFilteredPaymentTypes();
        protected List<SelectItemDto<int?>> CurrencyOptions { get; set; } = new();
        protected List<SelectItemDto<CashRegisterMovementType>> MovementTypeOptions { get; set; } = new()
        {
            new SelectItemDto<CashRegisterMovementType> { Text = "Kasa Girişi", Value = CashRegisterMovementType.In },
            new SelectItemDto<CashRegisterMovementType> { Text = "Kasa Çıkışı", Value = CashRegisterMovementType.Out }
        };

        protected override async Task OnInitializedAsync()
        {
            await LoadDropdownsAsync();

            if (Id.HasValue && Id.Value > 0)
            {
                var response = await MovementService.GetById(Id.Value);
                if (response.Ok && response.Result != null)
                    Model = response.Result;
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                    DialogService.Close(false);
                }
            }
            else
            {
                Model = new CashRegisterMovementUpsertDto
                {
                    MovementType = CashRegisterMovementType.In,
                    TransactionDate = DateTime.Now
                };
            }
        }

        private async Task LoadDropdownsAsync()
        {
            var cashRes = await CashRegisterService.GetCashRegisters();
            if (cashRes.Ok && cashRes.Result != null)
                CashRegisterList = cashRes.Result;

            var customerPager = new PageSetting(null, null, 0, 500);
            var customerRes = await CustomerService.GetPagedCustomers(customerPager);
            if (customerRes.Ok && customerRes.Result?.Data != null)
                CustomerList = customerRes.Result.Data.DistinctBy(c => c.Id).ToList();

            var salesPersonRes = await SalesPersonService.GetSalesPersons();
            if (salesPersonRes.Ok && salesPersonRes.Result != null)
                SalesPersonList = salesPersonRes.Result;

            var paymentRes = await PaymentTypeService.GetAllPaymentTypes();
            if (paymentRes.Ok && paymentRes.Result != null)
                PaymentTypeOptions = paymentRes.Result
                    .Select(p => new SelectItemDto<int?> { Text = p.Name, Value = p.Id })
                    .ToList();

            var currencyRes = await CurrencyService.GetCurrencies();
            if (currencyRes.Ok && currencyRes.Result != null)
            {
                var currencies = currencyRes.Result
                    .GroupBy(c => c.CurrencyCode)
                    .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                    .ToList();
                CurrencyOptions = currencies
                    .Select(c => new SelectItemDto<int?> { Text = $"{c.CurrencyCode} - {c.CurrencyName}", Value = c.Id })
                    .OrderBy(x => currencies.FirstOrDefault(c => c.Id == x.Value)?.CurrencyCode != "TRY")
                    .ThenBy(x => x.Text)
                    .ToList();
            }
        }

        private List<SelectItemDto<int?>> GetFilteredPaymentTypes()
        {
            if (Model == null || Model.CashRegisterId == 0)
                return PaymentTypeOptions;

            var selectedKasa = CashRegisterList.FirstOrDefault(c => c.Id == Model.CashRegisterId);
            if (selectedKasa?.PaymentTypeId != null && selectedKasa.PaymentTypeId.Value > 0)
            {
                var match = PaymentTypeOptions.Where(p => p.Value == selectedKasa.PaymentTypeId).ToList();
                return match.Any() ? match : PaymentTypeOptions;
            }
            return PaymentTypeOptions;
        }

        protected void OnCashRegisterChange(object? _)
        {
            if (Model == null || Model.CashRegisterId == 0) return;

            var kasa = CashRegisterList.FirstOrDefault(c => c.Id == Model.CashRegisterId);
            if (kasa == null) return;

            Model.CurrencyId = kasa.CurrencyId;
            if (kasa.PaymentTypeId.HasValue && kasa.PaymentTypeId.Value > 0)
                Model.PaymentTypeId = kasa.PaymentTypeId;

            InvokeAsync(StateHasChanged);
        }

        protected async Task FormSubmit(CashRegisterMovementUpsertDto args)
        {
            try
            {
                Saving = true;

                if (args.Id.HasValue && args.Id.Value > 0)
                {
                    var response = await MovementService.Update(new AuditWrapDto<CashRegisterMovementUpsertDto>
                    {
                        UserId = Security.User.Id,
                        Dto = args
                    });
                    if (response.Ok)
                    {
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kasa hareketi güncellendi.");
                        DialogService.Close(true);
                    }
                    else
                        NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                }
                else
                {
                    var response = await MovementService.Create(new AuditWrapDto<CashRegisterMovementUpsertDto>
                    {
                        UserId = Security.User.Id,
                        Dto = args
                    });
                    if (response.Ok)
                    {
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kasa hareketi oluşturuldu.");
                        DialogService.Close(true);
                    }
                    else
                        NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }
    }
}
