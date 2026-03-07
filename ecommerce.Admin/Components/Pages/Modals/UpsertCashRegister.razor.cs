using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services.Dtos;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Helpers;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCashRegister
    {
        [Parameter] public int? Id { get; set; }

        [Inject] protected DialogService DialogService { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] protected AuthenticationService Security { get; set; } = default!;
        [Inject] public ICashRegisterService Service { get; set; } = default!;
        [Inject] public ICurrencyAdminService CurrencyService { get; set; } = default!;
        [Inject] public ICorporationService CorporationService { get; set; } = default!;
        [Inject] public IBranchService BranchService { get; set; } = default!;
        [Inject] public IPaymentTypeService PaymentTypeService { get; set; } = default!;
        [Inject] public IBankAccountDefinitionService BankAccountService { get; set; } = default!;

        protected CashRegisterUpsertDto CashRegister = new();
        protected List<CurrencyListDto> Currencies = new();
        protected List<SelectItemDto<int?>> CurrencyOptions = new();
        protected List<CorporationListDto> Corporations = new();
        protected List<BranchListDto> Branches = new();
        protected List<SelectItemDto<int?>> PaymentTypeOptions = new();
        protected List<ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto.BankAccountListDto> BankAccountOptions = new();
        protected bool IsLoading = true;

        /// <summary>Ödeme tipi 3 veya 4 seçildiğinde banka hesabı dropdown gösterilir.</summary>
        private static readonly int[] BankPaymentTypeIds = { 3, 4 };
        protected bool ShowBankAccountDropdown => CashRegister.PaymentTypeId.HasValue && BankPaymentTypeIds.Contains(CashRegister.PaymentTypeId.Value);

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            IsLoading = true;
            try
            {
                var currencyRs = await CurrencyService.GetCurrencies();
                if (currencyRs.Ok && currencyRs.Result != null)
                {
                    Currencies = currencyRs.Result
                        .GroupBy(c => c.CurrencyCode)
                        .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                        .ToList();

                    CurrencyOptions = Currencies
                        .Select(c => new SelectItemDto<int?> { Text = $"{c.CurrencyCode} - {c.CurrencyName}", Value = c.Id })
                        .OrderBy(x => Currencies.FirstOrDefault(c => c.Id == x.Value)?.CurrencyCode != "TRY")
                        .ThenBy(x => x.Text)
                        .ToList();
                }

                var paymentTypeRs = await PaymentTypeService.GetAllPaymentTypes();
                if (paymentTypeRs.Ok && paymentTypeRs.Result != null)
                {
                    PaymentTypeOptions = paymentTypeRs.Result
                        .Select(pt => new SelectItemDto<int?> { Text = pt.Name, Value = pt.Id })
                        .ToList();
                }

                if (Id.HasValue)
                {
                    var result = await Service.GetCashRegisterById(Id.Value);
                    if (result.Ok)
                    {
                        CashRegister = result.Result;
                        if (CashRegister.CorporationId > 0)
                        {
                            await LoadBranches(CashRegister.CorporationId);
                        }
                        if (ShowBankAccountDropdown)
                        {
                            await LoadBankAccountsByPaymentType();
                        }
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, "Hata", "Kasa bilgileri yüklenemedi");
                    }
                }
                
                await LoadCorporations();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCorporations()
        {
            var result = await CorporationService.GetAllActiveCorporations();
            if (result.Ok) Corporations = result.Result ?? new List<CorporationListDto>();
        }

        private async Task LoadBranches(int corporationId)
        {
            var result = await BranchService.GetBranchesByCorporationId(corporationId);
            if (result.Ok) Branches = result.Result ?? new List<BranchListDto>();
            else Branches = new List<BranchListDto>();
        }

        protected async Task OnCorporationChange(object value)
        {
            CashRegister.BranchId = 0;
            if (value is int corpId)
            {
                await LoadBranches(corpId);
            }
            else
            {
                Branches = new List<BranchListDto>();
            }
        }

        protected async Task OnPaymentTypeChange(object value)
        {
            CashRegister.BankAccountId = null;
            if (ShowBankAccountDropdown)
            {
                await LoadBankAccountsByPaymentType();
            }
            else
            {
                BankAccountOptions = new List<ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto.BankAccountListDto>();
            }
        }

        private async Task LoadBankAccountsByPaymentType()
        {
            if (!CashRegister.PaymentTypeId.HasValue || !BankPaymentTypeIds.Contains(CashRegister.PaymentTypeId.Value))
            {
                BankAccountOptions = new List<ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto.BankAccountListDto>();
                return;
            }
            var result = await BankAccountService.GetBankAccountsByPaymentTypeIds(BankPaymentTypeIds);
            if (result.Ok && result.Result != null)
            {
                BankAccountOptions = result.Result;
            }
            else
            {
                BankAccountOptions = new List<ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto.BankAccountListDto>();
            }
        }

        protected async Task FormSubmit(CashRegisterUpsertDto args)
        {
            var result = await Service.UpsertCashRegister(new AuditWrapDto<CashRegisterUpsertDto>
            {
                UserId = Security.User.Id,
                Dto = args
            });

            if (result.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = Id.HasValue ? "Kasa güncellendi" : "Kasa oluşturuldu"
                });
                DialogService.Close(true);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", result.GetMetadataMessages());
            }
        }

        protected void CancelClick()
        {
            DialogService.Close(false);
        }
    }
}
