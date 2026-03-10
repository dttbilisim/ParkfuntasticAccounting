using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountExpenseDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountInstallmentDto;
using ecommerce.Admin.Domain.Dtos.ExpenseDto;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public record CashRegisterDisplayItem
{
    public int Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public class UpsertBankAccountModalBase : ComponentBase
{
    [Inject] protected IBankAccountDefinitionService BankAccountService { get; set; } = null!;
    [Inject] protected IExpenseDefinitionService ExpenseService { get; set; } = null!;
    [Inject] protected ICurrencyAdminService CurrencyService { get; set; } = null!;
    [Inject] protected IPaymentTypeService PaymentTypeService { get; set; } = null!;
    [Inject] protected ICashRegisterService CashRegisterService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;
    [Inject] protected AuthenticationService AuthenticationService { get; set; } = null!;

    [Parameter] public int Id { get; set; }

    protected BankAccountUpsertDto model { get; set; } = new();

    protected bool CanEditDetails => model.Id > 0;

    // Gider bağlantıları
    protected List<ExpenseDefinitionListDto> MainExpenses { get; set; } = new();
    protected List<ExpenseDefinitionListDto> SubExpenses { get; set; } = new();
    protected int? SelectedMainExpenseId { get; set; }
    protected int? SelectedSubExpenseId { get; set; }
    protected List<BankAccountExpenseListDto> ExpenseLinks { get; set; } = new();

    // Taksitler
    protected BankAccountInstallmentUpsertDto NewInstallment { get; set; } = new();
    protected List<BankAccountInstallmentListDto> Installments { get; set; } = new();

    // Currency
    protected List<CurrencyListDto> Currencies { get; set; } = new();

    // Payment Type Options (from PaymentType definitions)
    protected List<PaymentTypeListDto> PaymentTypeOptions { get; set; } = new();

    // Kasa listesi (PcPos transferde kullanılacak)
    protected List<CashRegisterDisplayItem> CashRegisters { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadModelAsync();
        await LoadMainExpensesAsync();
        await LoadCurrenciesAsync();
        await LoadPaymentTypesAsync();
        await LoadCashRegistersAsync();

        if (model.Id > 0)
        {
            await LoadExpenseLinksAsync();
            await LoadInstallmentsAsync();
        }
    }

    private async Task LoadModelAsync()
    {
        var result = await BankAccountService.GetBankAccountById(Id);
        if (!result.Ok || result.Result == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.GetMetadataMessages());
            return;
        }

        model = result.Result;
    }

    private async Task LoadMainExpensesAsync()
    {
        var result = await ExpenseService.GetMainExpenses(ExpenseOperationType.Gider);
        if (!result.Ok || result.Result == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.GetMetadataMessages());
            return;
        }

        MainExpenses = result.Result;
    }

    private async Task LoadCurrenciesAsync()
    {
        var result = await CurrencyService.GetCurrencies();
        if (result != null && result.Ok && result.Result != null)
        {
            Currencies = result.Result;
        }
        else if (result != null)
        {
            NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        }
    }

    private async Task LoadPaymentTypesAsync()
    {
        var result = await PaymentTypeService.GetAllPaymentTypes();
        if (result != null && result.Ok && result.Result != null)
        {
            PaymentTypeOptions = result.Result.Where(p => p.IsActive).ToList();
        }
    }

    private async Task LoadCashRegistersAsync()
    {
        var result = await CashRegisterService.GetCashRegisters();
        if (result != null && result.Ok && result.Result != null)
        {
            CashRegisters = result.Result
                .Select(c => new CashRegisterDisplayItem { Id = c.Id, DisplayName = $"{c.Name} ({c.CurrencyCode ?? ""})" })
                .ToList();
        }
    }

    protected async Task OnMainExpenseChanged(object value)
    {
        SelectedMainExpenseId = value as int?;
        await LoadSubExpensesAsync();
    }

    private async Task LoadSubExpensesAsync()
    {
        SubExpenses.Clear();
        SelectedSubExpenseId = null;

        if (SelectedMainExpenseId == null)
            return;

        var result = await ExpenseService.GetSubExpenses(SelectedMainExpenseId.Value);
        if (!result.Ok || result.Result == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.GetMetadataMessages());
            return;
        }

        SubExpenses = result.Result;
    }

    private async Task LoadExpenseLinksAsync()
    {
        var result = await BankAccountService.GetBankAccountExpenses(model.Id);
        if (!result.Ok || result.Result == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.GetMetadataMessages());
            return;
        }

        ExpenseLinks = result.Result;
    }

    private async Task LoadInstallmentsAsync()
    {
        var result = await BankAccountService.GetBankAccountInstallments(model.Id);
        if (!result.Ok || result.Result == null)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.GetMetadataMessages());
            return;
        }

        Installments = result.Result;
    }

    protected async Task AddExpenseLinkAsync()
    {
        if (SelectedMainExpenseId == null)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Uyarı", "Ana gider seçiniz.");
            return;
        }

        var dto = new BankAccountExpenseUpsertDto
        {
            BankAccountId = model.Id,
            MainExpenseId = SelectedMainExpenseId.Value,
            SubExpenseId = SelectedSubExpenseId
        };

        var audit = new AuditWrapDto<BankAccountExpenseUpsertDto>
        {
            UserId = AuthenticationService.User.Id,
            Dto = dto
        };
        var result = await BankAccountService.UpsertBankAccountExpense(audit);
        if (result != null && result.Ok)
        {
            await LoadExpenseLinksAsync();
            SelectedMainExpenseId = null;
            SelectedSubExpenseId = null;
        }
        else if (result != null)
        {
            NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        }
    }

    protected async Task DeleteExpenseLinkAsync(BankAccountExpenseListDto item)
    {
        var audit = new AuditWrapDto<BankAccountExpenseDeleteDto>
        {
            UserId = AuthenticationService.User.Id,
            Dto = new BankAccountExpenseDeleteDto { Id = item.Id }
        };
        var result = await BankAccountService.DeleteBankAccountExpense(audit);
        if (result != null && result.Ok)
        {
            await LoadExpenseLinksAsync();
        }
        else if (result != null)
        {
            NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        }
    }

    protected async Task AddInstallmentAsync()
    {
        if (NewInstallment.Installment <= 0)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Uyarı", "Taksit sayısı giriniz.");
            return;
        }

        NewInstallment.BankAccountId = model.Id;
        var audit = new AuditWrapDto<BankAccountInstallmentUpsertDto>
        {
            UserId = AuthenticationService.User.Id,
            Dto = NewInstallment
        };
        var result = await BankAccountService.UpsertBankAccountInstallment(audit);
        if (result != null && result.Ok)
        {
            NewInstallment = new BankAccountInstallmentUpsertDto { BankAccountId = model.Id };
            await LoadInstallmentsAsync();
        }
        else if (result != null)
        {
            NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        }
    }

    protected async Task DeleteInstallmentAsync(BankAccountInstallmentListDto item)
    {
        var audit = new AuditWrapDto<BankAccountInstallmentDeleteDto>
        {
            UserId = AuthenticationService.User.Id,
            Dto = new BankAccountInstallmentDeleteDto { Id = item.Id }
        };
        var result = await BankAccountService.DeleteBankAccountInstallment(audit);
        if (result != null && result.Ok)
        {
            await LoadInstallmentsAsync();
        }
        else if (result != null)
        {
            NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
        }
    }

    protected async Task Save(BankAccountUpsertDto args)
    {
        try
        {
            // Radzen Submit args bazen güncel değerleri taşımıyor; @bind-Value ile güncellenen model kullan
            var audit = new AuditWrapDto<BankAccountUpsertDto>
            {
                UserId = AuthenticationService.User.Id,
                Dto = model
            };
            var result = await BankAccountService.UpsertBankAccount(audit);
            if (result != null && result.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Banka hesabı kaydedildi."
                });
                DialogService.Close(model);
            }
            else if (result != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }

    protected void Close()
    {
        DialogService.Close(null);
    }

    protected void ShowValidationErrors()
    {
        NotificationService.Notify(NotificationSeverity.Warning, "Uyarı", "Lütfen tüm zorunlu alanları doldurunuz.");
    }
}


