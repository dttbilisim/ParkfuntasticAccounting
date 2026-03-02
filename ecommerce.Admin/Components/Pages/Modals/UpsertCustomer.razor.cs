using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Dtos.RegionDto;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;
using ecommerce.Core.Extensions;
using ecommerce.Core.Utils.ResultSet;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertCustomer : ComponentBase
{
    [Inject] private ICustomerService CustomerService { get; set; } = default!;
    [Inject] private ICityService CityService { get; set; } = default!;
    [Inject] private ITownService TownService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private IRegionService RegionService { get; set; } = default!;
    [Inject] private ISalesPersonService SalesPersonService { get; set; } = default!;
    [Inject] private IBranchService BranchService { get; set; } = default!;
    [Inject] private ICorporationService CorporationService { get; set; } = default!;
    [Inject] private IValidator<CustomerUpsertDto> CustomerValidator { get; set; } = default!;

    [Parameter] public int? Id { get; set; }

    private CustomerUpsertDto Customer { get; set; } = new();
    private RadzenFluentValidator<CustomerUpsertDto> CustomerFluentValidator { get; set; } = default!;
    private int _selectedTabIndex;
    private bool Saving { get; set; }

    /// <summary>
    /// Geçersiz submit: Hatalı alanın bulunduğu sekmeyi açar.
    /// </summary>
    private async Task OnValidationFailed()
    {
        if (CustomerFluentValidator == null) return;
        var errors = CustomerFluentValidator.GetValidationMessages();
        var firstProperty = errors.Keys.FirstOrDefault();
        if (string.IsNullOrEmpty(firstProperty)) return;
        var tabIndex = GetTabIndexForProperty(firstProperty);
        if (tabIndex < 0) return;
        _selectedTabIndex = tabIndex;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Property adına göre sekme indeksi: 0 Genel, 1 Cari Parametreleri, 2 İletişim Adres, 3 Adres Yönetimi, 4 Kullanıcı, 5 Şube.
    /// </summary>
    private static int GetTabIndexForProperty(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return 0;
        var p = propertyName.Trim();
        if (string.Equals(p, "Branches", StringComparison.OrdinalIgnoreCase)) return 5;
        var contact = new[] { "Email", "Mobile", "Phone", "CityId", "City Id", "TownId", "Town Id", "Address" };
        if (contact.Any(c => string.Equals(p, c, StringComparison.OrdinalIgnoreCase))) return 2;
        return 0; // Genel: Code, Type, Name, RegionId, TaxOffice, TaxNumber
    }
    
    private IEnumerable<CityListDto> Cities { get; set; } = new List<CityListDto>();
    private IEnumerable<TownListDto> Towns { get; set; } = new List<TownListDto>();
    private IEnumerable<RegionListDto> Regions { get; set; } = new List<RegionListDto>();
    private IEnumerable<CorporationListDto> Corporations { get; set; } = new List<CorporationListDto>();
    private IEnumerable<CustomerTypeOption> CustomerTypeOptions { get; set; } = Array.Empty<CustomerTypeOption>();
    private List<KeyValuePair<string, CustomerWorkingTypeEnum>> customerWorkingTypes = new();

    // Şube Yetkileri
    private IEnumerable<BranchListDto> AvailableBranches { get; set; } = new List<BranchListDto>();
    private int? SelectedBranchId { get; set; }
    private int? SelectedBranchCorporationId { get; set; }
    private RadzenDataGrid<CustomerBranchUpsertDto> branchGrid;

    // Plasiyer tanımları
    private IEnumerable<SalesPersonListDto> SalesPersons { get; set; } = new List<SalesPersonListDto>();
    private List<CustomerSalesPersonDto> CustomerSalesPersons { get; set; } = new();
    private int? SelectedSalesPersonId { get; set; }
    private RadzenDataGrid<CustomerSalesPersonDto> salesPersonGrid;

    // Kullanıcı yönetimi
    private IEnumerable<IdentityUserListDto> AllUsers { get; set; } = new List<IdentityUserListDto>();
    private List<IdentityUserListDto> CustomerUsers { get; set; } = new();
    private int? SelectedUserId { get; set; }

    // Adres yönetimi
    private List<ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressListDto> CustomerAddresses { get; set; } = new();
    private ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressUpsertDto? CurrentAddress { get; set; }
    private bool IsAddressModalOpen { get; set; } = false;
    private bool IsEditingAddress { get; set; } = false;
    private IEnumerable<TownListDto> AddressTowns { get; set; } = new List<TownListDto>();

    protected override async Task OnInitializedAsync()
    {
        LoadCustomerTypes();
        LoadCustomerWorkingTypes();
        await LoadCities();
        await LoadRegions();
        await LoadCorporations();
        await LoadSalesPersons();
        
        if (Id.HasValue && Id.Value > 0)
        {
            var result = await CustomerService.GetCustomerById(Id.Value);
            if (result.Ok)
            {
                Customer = result.Result!;
                if (Customer.CityId.HasValue)
                {
                    await LoadTowns(Customer.CityId.Value);
                }

                await LoadCustomerSalesPersons(Customer.Id ?? Id.Value);
                await LoadCustomerUsers(Customer.Id ?? Id.Value);
                await LoadCustomerAddresses(Customer.Id ?? Id.Value);
                
                // Load users filtered by customer's corporation
                await LoadAllUsers();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = result.Metadata?.Message ?? "Cari bilgileri yüklenemedi"
                });
                DialogService.Close();
            }
        }
        else
        {
            // For new customer, load users (will use current tenant corporation internally)
            await LoadAllUsers();
            
            var codeResult = await CustomerService.GetNextCustomerCode();
            if (codeResult.Ok)
            {
                Customer.Code = codeResult.Result;
            }
            // Yeni kayıt için default değerleri ayarla
            if (Customer.CustomerWorkingType == CustomerWorkingTypeEnum.Pesin)
            {
                Customer.Vade = 0;
            }
            else if (Customer.CustomerWorkingType == CustomerWorkingTypeEnum.Vadeli)
            {
                Customer.Vade = 30;
            }
        }
    }
    

    private async Task LoadCities()
    {
        var result = await CityService.GetCities();
        if (result.Ok)
        {
            Cities = result.Result!;
        }
    }

    private void LoadCustomerTypes()
    {
        CustomerTypeOptions = Enum.GetValues(typeof(CustomerType))
            .Cast<CustomerType>()
            .Select(ct => new CustomerTypeOption
            {
                Value = ct,
                Text = ct switch
                {
                    CustomerType.Buyer => "Alıcı",
                    CustomerType.Seller => "Satıcı",
                    CustomerType.BuyerSeller => "Alıcı + Satıcı",
                    CustomerType.Employee => "Personel",
                    CustomerType.Other => "Diğer",
                    _ => ct.ToString()
                }
            })
            .ToList();
    }

    private void LoadCustomerWorkingTypes()
    {
        customerWorkingTypes = Enum.GetValues(typeof(CustomerWorkingTypeEnum))
            .Cast<CustomerWorkingTypeEnum>()
            .Select(cwt => new KeyValuePair<string, CustomerWorkingTypeEnum>(
                cwt.GetDisplayName(),
                cwt
            ))
            .ToList();
    }

    private async Task OnCustomerWorkingTypeChanged(object value)
    {
        if (value is CustomerWorkingTypeEnum workingType)
        {
            Customer.CustomerWorkingType = workingType;
            
            // Peşin seçilirse 0, Vadeli seçilirse 30 at
            if (workingType == CustomerWorkingTypeEnum.Pesin)
            {
                Customer.Vade = 0;
            }
            else if (workingType == CustomerWorkingTypeEnum.Vadeli)
            {
                Customer.Vade = 30;
            }
            
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadRegions()
    {
        var result = await RegionService.GetRegions();
        if (result.Ok)
        {
            Regions = result.Result!;
        }
    }

    private async Task LoadCorporations()
    {
        var result = await CorporationService.GetAllActiveCorporations();
        if (result.Ok)
        {
            Corporations = result.Result!;
        }
    }

    private async Task OnBranchCorporationChange(object value)
    {
        SelectedBranchId = null;
        if (value is int corpId)
        {
            var result = await BranchService.GetBranchesByCorporationId(corpId);
            if (result.Ok)
            {
                AvailableBranches = result.Result!;
            }
        }
        else
        {
            AvailableBranches = new List<BranchListDto>();
        }
    }

    private async Task AddBranchMapping()
    {
        if (!SelectedBranchId.HasValue || !SelectedBranchCorporationId.HasValue) return;

        if (Customer.Branches.Any(b => b.BranchId == SelectedBranchId.Value))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Uyarı", "Bu şube zaten eklenmiş.");
            return;
        }

        var branch = AvailableBranches.FirstOrDefault(b => b.Id == SelectedBranchId.Value);
        var corp = Corporations.FirstOrDefault(c => c.Id == SelectedBranchCorporationId.Value);

        Customer.Branches.Add(new CustomerBranchUpsertDto
        {
            BranchId = SelectedBranchId.Value,
            BranchName = branch?.Name,
            CorporationId = SelectedBranchCorporationId.Value,
            CorporationName = corp?.Name,
            IsDefault = !Customer.Branches.Any()
        });

        if (branchGrid != null) await branchGrid.Reload();
        SelectedBranchId = null;
    }

    private async Task RemoveBranchMapping(CustomerBranchUpsertDto mapping)
    {
        Customer.Branches.Remove(mapping);
        if (branchGrid != null) await branchGrid.Reload();
    }

    private void OnDefaultBranchChange(CustomerBranchUpsertDto mapping)
    {
        foreach (var b in Customer.Branches)
        {
            b.IsDefault = b == mapping;
        }
    }

     private async Task LoadTowns(int cityId)
    {
        var result = await TownService.GetTownsByCityId(cityId);
        if (result.Ok)
        {
            Towns = result.Result!;
        }
        else 
        {
             Towns = new List<TownListDto>();
        }
    }

    private async Task OnCityChange(object value)
    {
        Customer.TownId = null;
        if (value is int cityId)
        {
            await LoadTowns(cityId);
        }
        else
        {
            Towns = new List<TownListDto>();
        }
    }


    private async Task LoadSalesPersons()
    {
        var result = await SalesPersonService.GetSalesPersons();
        if (result.Ok && result.Result != null)
        {
            SalesPersons = result.Result;
        }
    }

    private async Task LoadCustomerSalesPersons(int customerId)
    {
        var result = await CustomerService.GetCustomerSalesPersons(customerId);
        if (result.Ok && result.Result != null)
        {
            CustomerSalesPersons = result.Result;
        }
        else
        {
            CustomerSalesPersons = new List<CustomerSalesPersonDto>();
        }
    }

    private async Task AddSalesPersonMapping()
    {
        if (!Id.HasValue || Id.Value <= 0 || !SelectedSalesPersonId.HasValue)
            return;

        var rs = await CustomerService.AddSalesPersonToCustomer(Id.Value, SelectedSalesPersonId.Value);
        if (rs.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Plasiyer cari ile ilişkilendirildi.");
            await LoadCustomerSalesPersons(Id.Value);
            if (salesPersonGrid != null) await salesPersonGrid.Reload();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
        }
    }

    private async Task RemoveSalesPersonMapping(CustomerSalesPersonDto mapping)
    {
        var confirm = await DialogService.Confirm(
            $"{mapping.SalesPersonName} plasiyer bağlantısını kaldırmak istediğinize emin misiniz?",
            "Onay",
            new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

        if (confirm != true)
            return;

        var rs = await CustomerService.RemoveSalesPersonFromCustomer(mapping.Id);
        if (rs.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Plasiyer bağlantısı kaldırıldı.");
            if (Id.HasValue)
            {
                await LoadCustomerSalesPersons(Id.Value);
                if (salesPersonGrid != null) await salesPersonGrid.Reload();
            }
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
        }
    }

    private async Task OnDefaultSalesPersonChange(CustomerSalesPersonDto mapping, bool isChecked)
    {
        if (!Id.HasValue) return;

        // Prevent unchecking the currently default item directly
        if (!isChecked)
        {
            // Update the item in the list directly to ensure grid reflects true
            var currentItem = CustomerSalesPersons.FirstOrDefault(x => x.Id == mapping.Id);
            if (currentItem != null) currentItem.IsDefault = true;
            
            if (salesPersonGrid != null) await salesPersonGrid.Reload();
            NotificationService.Notify(NotificationSeverity.Info, "Bilgi", "Varsayılan plasiyeri değiştirmek için başka bir plasiyer seçiniz.");
            return;
        }

        // Optimistic update: Set all others to false AND find/update target
        foreach (var item in CustomerSalesPersons)
        {
            item.IsDefault = (item.Id == mapping.Id);
        }
        
        if (salesPersonGrid != null) await salesPersonGrid.Reload(); // Refresh visualization immediately

        // Backend call
        var rs = await CustomerService.SetDefaultSalesPerson(Id.Value, mapping.Id);
        if (rs.Ok)
        {
            await LoadCustomerSalesPersons(Id.Value);

             // DEBUG CHECK
            var updatedItem = CustomerSalesPersons.FirstOrDefault(x => x.Id == mapping.Id);
            if (updatedItem != null)
            {
               // NotificationService.Notify(NotificationSeverity.Info, "Debug", $"DB'den gelen değer: ID={updatedItem.Id}, IsDefault={updatedItem.IsDefault}");
            }
            
            if (salesPersonGrid != null) await salesPersonGrid.Reload();
            StateHasChanged();
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, rs.GetMetadataMessages());
            // Revert changes if error
            await LoadCustomerSalesPersons(Id.Value);
            if (salesPersonGrid != null) await salesPersonGrid.Reload();
        }
    }

    private async Task FormSubmit(CustomerUpsertDto args)
    {
        Saving = true;
        try
        {
            var result = await CustomerService.UpsertCustomer(args);
            if (result.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Cari başarıyla kaydedildi."
                });
                DialogService.Close(true);
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = result.Metadata?.Message ?? "Bir hata oluştu"
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Beklenmeyen bir hata oluştu."
            });
        }
        finally
        {
            Saving = false;
        }
    }

    private async Task LoadAllUsers()
    {
        var result = await CustomerService.GetAllUsers(Customer.CorporationId > 0 ? Customer.CorporationId : null);
        if (result.Ok)
        {
            AllUsers = result.Result!;
        }
    }

    private async Task LoadCustomerUsers(int customerId)
    {
        var result = await CustomerService.GetCustomerUsers(customerId);
        if (result.Ok)
        {
            CustomerUsers = result.Result!;
        }
    }

    private async Task LinkUser()
    {
        if (!Id.HasValue || !SelectedUserId.HasValue)
            return;

        var result = await CustomerService.LinkUserToCustomer(SelectedUserId.Value, Id.Value);
        if (result.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kullanıcı başarıyla bağlandı.");
            await LoadCustomerUsers(Id.Value);
            SelectedUserId = null;
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "İşlem başarısız.");
        }
    }

    private async Task UnlinkUser(int userId)
    {
        var confirm = await DialogService.Confirm("Kullanıcı bağlantısını kaldırmak istediğinize emin misiniz?", "Onay");
        if (confirm != true) return;

        var result = await CustomerService.UnlinkUserFromCustomer(userId);
        if (result.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kullanıcı bağlantısı kaldırıldı.");
            if (Id.HasValue) await LoadCustomerUsers(Id.Value);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "İşlem başarısız.");
        }
    }

    // ========== Adres Yönetimi ==========

    private async Task LoadCustomerAddresses(int customerId)
    {
        try
        {
            var result = await CustomerService.GetCustomerAddresses(customerId);
            if (result.Ok && result.Result != null)
            {
                CustomerAddresses = result.Result;
            }
            else
            {
                CustomerAddresses = new List<ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressListDto>();
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Adresler yüklenirken bir hata oluştu.");
        }
    }

    private async Task OpenAddAddressModal()
    {
        CurrentAddress = new ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressUpsertDto();
        IsEditingAddress = false;
        await OpenAddressDialog();
    }

    private async Task OpenEditAddressModal(int addressId)
    {
        try
        {
            var result = await CustomerService.GetCustomerAddressById(addressId);
            if (result.Ok && result.Result != null)
            {
                CurrentAddress = result.Result;
                IsEditingAddress = true;
                await OpenAddressDialog();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "Adres bulunamadı.");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Adres yüklenirken bir hata oluştu.");
        }
    }

    private async Task OpenAddressDialog()
    {
        if (CurrentAddress == null) return;
        
        // Load towns if city is selected
        if (CurrentAddress.CityId.HasValue)
        {
            var result = await TownService.GetTownsByCityId(CurrentAddress.CityId.Value);
            if (result.Ok)
            {
                AddressTowns = result.Result!;
            }
        }

        var result2 = await DialogService.OpenAsync<UpsertCustomerAddress>(
            IsEditingAddress ? "Adres Düzenle" : "Yeni Adres Ekle",
            new Dictionary<string, object>
            {
                { "Address", CurrentAddress },
                { "Cities", Cities },
                { "Towns", AddressTowns },
                { "OnSave", EventCallback.Factory.Create<ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressUpsertDto>(this, async (address) => { await SaveAddressFromDialog(address); }) }
            },
            new DialogOptions { Width = "700px", Resizable = true, Draggable = true, Style = "padding: 0;" });

        if (result2 != null)
        {
            await LoadCustomerAddresses(Id!.Value);
        }
    }

    private async Task SaveAddressFromDialog(ecommerce.Admin.Domain.Dtos.UserAddressDto.UserAddressUpsertDto address)
    {
        CurrentAddress = address;
        await SaveAddress();
    }

    private async Task SaveAddress()
    {
        if (CurrentAddress == null || !Id.HasValue) return;

        try
        {
            IActionResult<Empty> result;
            if (IsEditingAddress && CurrentAddress.Id.HasValue)
            {
                result = await CustomerService.UpdateCustomerAddress(CurrentAddress.Id.Value, CurrentAddress);
            }
            else
            {
                var addResult = await CustomerService.AddCustomerAddress(Id.Value, CurrentAddress);
                result = addResult.Ok ? OperationResult.CreateResult<Empty>() : new IActionResult<Empty> { Metadata = addResult.Metadata };
            }

            if (result.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Başarılı", IsEditingAddress ? "Adres güncellendi." : "Adres eklendi.");
                IsAddressModalOpen = false;
                CurrentAddress = null;
                await LoadCustomerAddresses(Id.Value);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "İşlem başarısız.");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Adres kaydedilirken bir hata oluştu.");
        }
    }

    private async Task DeleteAddress(int addressId)
    {
        var confirm = await DialogService.Confirm(
            "Bu adresi silmek istediğinize emin misiniz?",
            "Onay",
            new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

        if (confirm != true) return;

        try
        {
            var result = await CustomerService.DeleteCustomerAddress(addressId);
            if (result.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Adres silindi.");
                if (Id.HasValue) await LoadCustomerAddresses(Id.Value);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "İşlem başarısız.");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Adres silinirken bir hata oluştu.");
        }
    }

    private async Task SetDefaultAddress(int addressId)
    {
        if (!Id.HasValue) return;

        try
        {
            var result = await CustomerService.SetDefaultCustomerAddress(Id.Value, addressId);
            if (result.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Varsayılan adres ayarlandı.");
                await LoadCustomerAddresses(Id.Value);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", result.Metadata?.Message ?? "İşlem başarısız.");
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", "Varsayılan adres ayarlanırken bir hata oluştu.");
        }
    }
}

public class CustomerTypeOption
{
    public CustomerType Value { get; set; }
    public string Text { get; set; } = string.Empty;
}
