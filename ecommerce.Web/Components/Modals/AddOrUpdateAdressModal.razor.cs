using Blazored.LocalStorage;
using Blazored.Modal;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities; // For City and Town
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using System.Threading;
using Blazored.Modal.Services;
using ecommerce.Web.Validators;

namespace ecommerce.Web.Components.Modals;

public partial class AddOrUpdateAdressModal
{
    [Parameter] public UserAddress EditableAddress { get; set; }
    [CascadingParameter] public Blazored.Modal.BlazoredModalInstance ModalInstance { get; set; }  
    [Inject] private ICommonManager _commonManager { get; set; }
    [Inject] private IUserManager _userManager { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private ILocalStorageService _localStorage { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }

    private List<CityListDto> CityList { get; set; } = new();
    private List<TownListDto> AllTownList { get; set; } = new();
    private List<TownListDto> TownList { get; set; } = new();
    
    private List<CityListDto> InvoiceCityList { get; set; } = new();
    private List<TownListDto> InvoiceTownList { get; set; } = new();
    private User CurrentUser { get; set; }

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private bool isLoading = false;
    private bool _prefilled = false;

    protected override async Task OnInitializedAsync()
    {
        var userResult = await _userManager.GetCurrentUserAsync();
        if (userResult.Ok && userResult.Result != null)
        {
            CurrentUser = userResult.Result;
        }
        await LoadCitiesAndTowns();
    }

    protected override async Task OnParametersSetAsync()
    {
        
            if (CityList.Count == 0)
            {
                await LoadCitiesAndTowns();
            }
            await PrefillSelectionsAsync();
        
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var localLanguage = await _localStorage.GetItemAsync<string>("lang");
            if (localLanguage != null)
            {
                _appStateManager.InvokeLanguageChanged(localLanguage);
                lang.Language = lang.Languages.FirstOrDefault(x => x.Locale == localLanguage);
            }


            StateHasChanged();
        }

   
        if (!_prefilled && EditableAddress != null)
        {
            await PrefillSelectionsAsync();
        }
    }

    private async Task LoadCitiesAndTowns()
    {
        try
        {
            // Load cities from API
            var citiesResult = await _commonManager.GetCategoryList();
            if (citiesResult.Ok && citiesResult.Result != null)
            {
                CityList = citiesResult.Result
                    .OrderBy(c => c.Name)
                    .ToList();
                    
                // Invoice cities same as delivery cities
                InvoiceCityList = CityList;
            }

            var townsResult = await _commonManager.GetTownList();
            if (townsResult.Ok && townsResult.Result != null)
            {
                AllTownList = townsResult.Result
                    .OrderBy(t => t.Name)
                    .ToList();
            }

            UpdateTownListForCity(EditableAddress?.CityId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading cities and towns: {ex.Message}");
            var cityFallback = await _commonManager.GetCategoryList();
            if (cityFallback.Result != null)
            {
                CityList = cityFallback.Result
                    .OrderBy(c => c.Name)
                    .ToList();
            }

            var townFallback = await _commonManager.GetTownList();
            if (townFallback.Result != null)
            {
                AllTownList = townFallback.Result
                    .OrderBy(t => t.Name)
                    .ToList();
            }

            UpdateTownListForCity(EditableAddress?.CityId);
        }
    }

    private async Task PrefillSelectionsAsync()
    {
        if (isLoading) return;
        await _loadLock.WaitAsync();
        try
        {
            isLoading = true;
            if (EditableAddress != null)
            {
                if (!EditableAddress.CityId.HasValue && EditableAddress.City != null)
                {
                    EditableAddress.CityId = EditableAddress.City.Id;
                }

                if (!EditableAddress.TownId.HasValue && EditableAddress.Town != null)
                {
                    EditableAddress.TownId = EditableAddress.Town.Id;
                }
            }

            if (EditableAddress?.CityId.HasValue == true)
            {
                UpdateTownListForCity(EditableAddress.CityId);
            }
            else
            {
                TownList = new List<TownListDto>();
            }
            _prefilled = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during prefill: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            _loadLock.Release();
            StateHasChanged();
        }
    }

    private async Task OnCityChanged(object value)
    {
        await _loadLock.WaitAsync();
        try
        {
            isLoading = true;
            int? cityValue = value as int?;
            if (!cityValue.HasValue && value is int boxedInt)
            {
                cityValue = boxedInt;
            }

            if (cityValue.HasValue)
            {
                await OnCityChanged(cityValue.Value);
            }
            else
            {
                EditableAddress.CityId = null;
                UpdateTownListForCity(null);
            }
        }
        finally
        {
            isLoading = false;
            _loadLock.Release();
            StateHasChanged();
        }
    }

    private void UpdateTownListForCity(int? cityId)
    {
        if (cityId.HasValue)
        {
            TownList = AllTownList
                .Where(t => t.CityId == cityId.Value)
                .OrderBy(t => t.Name)
                .ToList();
        }
        else
        {
            TownList = new List<TownListDto>();
        }
    }

    private async Task OnCityChanged(int value)
    {
        EditableAddress.CityId = value;
        EditableAddress.TownId = null;
        UpdateTownListForCity(value);
        StateHasChanged();
    }

    private async Task OnTownChanged(object args)
    {
        if (args != null)
        {
            var townId = args as int?;
            if (townId != null)
            {
                EditableAddress.TownId = townId.Value;
            }
        }
    }
    
    private async Task OnInvoiceCityChanged(object args)
    {
        if (args != null)
        {
            var cityId = args as int?;
            if (cityId != null)
            {
                EditableAddress.InvoiceCityId = cityId.Value;
                
                // Load invoice towns for selected city
                InvoiceTownList = AllTownList
                    .Where(t => t.CityId == cityId.Value)
                    .OrderBy(t => t.Name)
                    .ToList();
                StateHasChanged();
            }
        }
    }
    
    private async Task OnInvoiceTownChanged(object args)
    {
        if (args != null)
        {
            var townId = args as int?;
            if (townId != null)
            {
                EditableAddress.InvoiceTownId = townId.Value;
            }
        }
    }

    private async Task CloseModal()
    {
        await ModalInstance.CancelAsync();
    }

    private async Task SaveChanges()
    {
        try
        {
            await _jsRuntime.ShowFullPageLoader();
            if (EditableAddress == null)
                return;

            // FluentValidation
            var validator = new UserAddressValidator();
            var validationResult = await validator.ValidateAsync(EditableAddress);
            
            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                _notificationService.Notify(NotificationSeverity.Error, firstError?.ErrorMessage ?? "Doğrulama hatası");
                return;
            }

            // Validate IdentityNumber (TC Kimlik No) if provided
            if (!string.IsNullOrWhiteSpace(EditableAddress.IdentityNumber) && 
                !_appStateManager.IsValidTCKN(EditableAddress.IdentityNumber))
            {
                _notificationService.Notify(NotificationSeverity.Error, "Geçersiz TC Kimlik Numarası! TC Kimlik No 11 haneli ve geçerli bir numara olmalıdır.");
                return;
            }

            // Validate User tax fields for B2B users
            if (CurrentUser != null && CurrentUser.WebUserType == Core.Utils.WebUserType.B2B)
            {
                // Tax Number validation for B2B business partners
                if (!string.IsNullOrWhiteSpace(CurrentUser.VatNumber) && 
                    !_appStateManager.IsValidTaxNumber(CurrentUser.VatNumber))
                {
                    _notificationService.Notify(NotificationSeverity.Error, "Geçersiz Vergi Numarası! Vergi numarası 10 haneli ve sadece rakamlardan oluşmalıdır.");
                    return;
                }
                
                // Save User VAT fields for B2B users
                var userUpdateResult = await _userManager.UpdateUserProfileAsync(CurrentUser);
                if (!userUpdateResult.Ok)
                {
                    Console.WriteLine("Failed to update user VAT fields");
                }
            }

            var result = await _userManager.UpsertUserAddressAsync(EditableAddress);

            if (!result.Ok)
            {
                _notificationService.Notify(NotificationSeverity.Error, "Bir hata oluştu!");
                return;
            }
            else
            {
                _notificationService.Notify(NotificationSeverity.Success, @lang["AdressSucces"]);
            }

            await ModalInstance.CloseAsync(ModalResult.Ok(EditableAddress));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await _jsRuntime.HideFullPageLoader();
        }
    }
}