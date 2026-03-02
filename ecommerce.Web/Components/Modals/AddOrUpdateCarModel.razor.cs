using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Services;
using ecommerce.Web.Utility;
using I18NPortable;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Http;
using Blazored.Modal;
using Blazored.Modal.Services;
using Radzen;

namespace ecommerce.Web.Components.Modals;

public partial class AddOrUpdateCarModel : IDisposable
{
    [Parameter] public UserCars? EditableCars { get; set; }
    [Inject] private IDotIntegrationService _dotService { get; set; }
    [Inject] private II18N lang { get; set; }
    [Inject] private IUserCarService _userCarService { get; set; }
    [Inject] private IJSRuntime _jsRuntime { get; set; }
    [Inject] private AppStateManager _appStateManager { get; set; }
    [Inject] private IHttpContextAccessor _httpContextAccessor { get; set; }
    [Inject] private NotificationService _notificationService { get; set; }
    [Inject] private IServiceScopeFactory _serviceScopeFactory { get; set; }
    [Inject] private IManufacturerCacheService _manufacturerCacheService { get; set; }
    [CascadingParameter] BlazoredModalInstance BlazoredModal { get; set; } = default!;
    private DotNetObjectReference<AddOrUpdateCarModel>? objRef;
    private readonly object _imagesLockObject = new object();
    
    // Current step tracking
    private int currentStep = 1;
    
    // Selected items
    private DotVehicleType? selectedVehicleType;
    private DotManufacturer? selectedManufacturer;
    private DotBaseModel? selectedBaseModel;
    private DotSubModel? selectedSubModel;
    private DotCarBodyOption? selectedCarBody;
    private DotEngineOption? selectedEngine;
    private List<DotOption> selectedOptions = new();
    
    // Data lists
    private List<DotVehicleType> vehicleTypes = new();
    private List<DotVehicleType> filteredVehicleTypes = new();
    private List<DotManufacturer> manufacturers = new();
    private List<DotManufacturer> filteredManufacturers = new();
    private List<DotManufacturer> displayManufacturers = new();
    private int brandRenderTick = 0;
    private List<DotBaseModel> baseModels = new();
    private List<DotBaseModel> filteredBaseModels = new();
    private List<DotSubModel> subModels = new();
    private List<DotSubModel> filteredSubModels = new();
    private List<DotCarBodyOption> carBodyOptions = new();
    private List<DotEngineOption> engineOptions = new();
    private List<DotOption> options = new();
    private Dictionary<string, string> vehicleTypeIcons = new();
    private List<VehicleImageWithCode> vehicleImages = new();
    private readonly Dictionary<int, string> subModelThumbnails = new();
    private static readonly HashSet<string> popularBrandNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Popular names normalized to match DB list shared by user
        "BMW","Mercedes-Benz","Audi","Volkswagen","Toyota","Honda","Ford","Opel","Renault","Fiat","Hyundai","Kia","Peugeot","Citroen","Citroën","Nissan","Volvo","Skoda","Seat","Alfa Romeo","Porsche","MINI","Mazda","Mitsubishi","Subaru","Suzuki","Dacia","Chevrolet","Land Rover","Jeep","Tesla","Jaguar","Lexus","Ferrari","Lamborghini","Rolls Royce","Bentley","Saab","Smart","Skoda"
    };

    private string _plateNumber = string.Empty;
    private string PlateNumber 
    {
        get => _plateNumber;
        set
        {
            if (_plateNumber != value)
            {
                _plateNumber = value;
                StateHasChanged(); // UI'ı güncellemek için
            }
        }
    }
    
    // Loading states
    private bool isInitializing = false; // Modal açılırken genel loading
    private bool isLoadingVehicleTypes = false;
    private bool isLoadingManufacturers = false;
    private bool isLoadingBaseModels = false;
    private bool isLoadingSubModels = false;
    private bool isLoadingModelThumbs = false;
    private bool isLoadingCarBodyOptions = false;
    private bool isLoadingEngineOptions = false;

    private bool isLoadingOptions = false;

    // Modal state
    private bool _isModalVisible;
    public bool IsModalVisible
    {
        get => _isModalVisible;
        set
        {
            if (_isModalVisible != value)
            {
                _isModalVisible = value;
                InvokeAsync(StateHasChanged);
            }
        }
    }
    private bool IsEditMode = false; // Yeni eklendi
    
    // Search
    private string _manufacturerSearch = "";
    private string manufacturerSearch
    {
        get => _manufacturerSearch;
        set
        {
            if (_manufacturerSearch != value)
            {
                _manufacturerSearch = value;
                if (string.IsNullOrEmpty(_manufacturerSearch) || _manufacturerSearch.Length >= 2)
                {
                    FilterManufacturers();
                }
            }
        }
    }
    private string _baseModelSearch = "";
    private string baseModelSearch
    {
        get => _baseModelSearch;
        set
        {
            if (_baseModelSearch != value)
            {
                _baseModelSearch = value;
                if (string.IsNullOrEmpty(_baseModelSearch) || _baseModelSearch.Length >= 2)
                {
                    FilterBaseModels();
                }
            }
        }
    }
    
    private string _subModelSearch = "";
    private string subModelSearch
    {
        get => _subModelSearch;
        set
        {
            if (_subModelSearch != value)
            {
                _subModelSearch = value;
                if (string.IsNullOrEmpty(_subModelSearch) || _subModelSearch.Length >= 2)
                {
                    FilterSubModels();
                }
            }
        }
    }
    
    protected override async Task OnInitializedAsync()
    {
        
        // Global loading başlat
        await _appStateManager.SetGlobalLoading(true);
        
        try
        {
            if (EditableCars != null && EditableCars.Id > 0)
            {
                IsEditMode = true;
                // Always load initial data first to ensure vehicleTypes are available
                await LoadInitialData();
                await LoadEditData();
            }
            else
            {
                IsEditMode = false;
                ResetForm();
                // Load initial data after reset
                await LoadInitialData();
            }
        }
        finally
        {
            // Global loading bitir
            await _appStateManager.SetGlobalLoading(false);
        }
        
        // Show modal
        IsModalVisible = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            objRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("carBrandLogos.setComponentReference", objRef);
            //await OpenModal(); // Modal'ı buradan açmıyoruz, UserDashboardPage kontrol ediyor
        }
    }

    public void Dispose()
    {
        // JavaScript tarafındaki reference'ı temizle
        _jsRuntime.InvokeVoidAsync("carBrandLogos.clearComponentReference");

        // DotNet reference'ı temizle
        objRef?.Dispose();
        objRef = null;
    }
    
    private async Task LoadInitialData()
    {
        await LoadVehicleTypes();
    }
    
    private async Task LoadVehicleTypes()
    {
        isLoadingVehicleTypes = true;
        StateHasChanged();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        var result = await dotService.GetVehicleTypesAsync();
        if (result.Ok)
        {
            vehicleTypes = result.Result ?? new List<DotVehicleType>();
            filteredVehicleTypes = vehicleTypes;
            // Yüklenen araç tipleri için ikonları önceden al ve cache'le
            foreach (var vehicleType in vehicleTypes)
            {
                try
                {
                    var iconClass = await _jsRuntime.InvokeAsync<string>("carBrandLogos.getVehicleTypeIcon", vehicleType.Name);
                    vehicleTypeIcons[vehicleType.Name] = iconClass ?? "fas fa-car";
                }
                catch (Exception ex)
                {
                    vehicleTypeIcons[vehicleType.Name] = "fas fa-car"; // Fallback
                }
            }
        }
        
        isLoadingVehicleTypes = false;
        StateHasChanged();
    }
    
    private async Task LoadManufacturers()
    {
        if (selectedVehicleType == null) return;
        
        isLoadingManufacturers = true;
        StateHasChanged();
        
        try
        {
            // Use ManufacturerCacheService to get cached manufacturers with logos
            var result = await _manufacturerCacheService.GetAllAsync();
            
            if (result.Ok && result.Result != null)
            {
                // Filter by vehicle type (Match manufacturer type OR have any model of that type)
                var filteredDtos = result.Result
                    .Where(m => m.VehicleType == selectedVehicleType.Id || (m.Models != null && m.Models.Any(mod => mod.VehicleType == selectedVehicleType.Id)))
                    .ToList();

                var mappedManufacturers = filteredDtos.Select(dto => new DotManufacturer
                {
                    Id = dto.Id,
                    DatKey = dto.DatKey,
                    Name = dto.Name,
                    VehicleType = dto.VehicleType,
                    LogoUrl = dto.LogoUrl,
                    Order = dto.Order,
                    IsActive = true
                }).ToList();

                var ordered = OrderManufacturersForDisplay(mappedManufacturers).ToList();
                manufacturers = ordered;
                filteredManufacturers = ordered;
                UpdateManufacturerDisplayList();
            }
            else
            {
                manufacturers = new List<DotManufacturer>();
                filteredManufacturers = new List<DotManufacturer>();
                displayManufacturers = new List<DotManufacturer>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading manufacturers: {ex.Message}");
            manufacturers = new List<DotManufacturer>();
            filteredManufacturers = new List<DotManufacturer>();
            displayManufacturers = new List<DotManufacturer>();
        }
        finally
        {
            isLoadingManufacturers = false;
            StateHasChanged();
        }
    }


    
    
    private async Task LoadBaseModels()
    {
        if (selectedManufacturer == null || selectedVehicleType == null) return;
        
        isLoadingBaseModels = true;
        StateHasChanged();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        // Use selectedVehicleType.Id to ensure we get models for the *selected* type (e.g. Car), 
        // not necessarily the manufacturer's default type (e.g. Commercial).
        var result = await dotService.GetBaseModelsByManufacturerAsync(selectedManufacturer.DatKey, selectedVehicleType.Id);
        if (result.Ok)
        {
            baseModels = result.Result ?? new List<DotBaseModel>();
            FilterBaseModels(); // Arama filtrelerini uygula
        }
        
        isLoadingBaseModels = false;
        StateHasChanged();
    }
    
    private async Task LoadSubModels()
    {
        if (selectedBaseModel == null || selectedManufacturer == null || selectedVehicleType == null) return;
        
        isLoadingSubModels = true;
        StateHasChanged();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        var result = await dotService.GetSubModelsByBaseModelAsync(selectedManufacturer.DatKey, selectedBaseModel.DatKey, selectedVehicleType.Id);
        if (result.Ok)
        {
            subModels = result.Result ?? new List<DotSubModel>();
            filteredSubModels = subModels; // Initialize filteredSubModels
            FilterSubModels(); // Arama filtrelerini uygula
            // Liste güncellendikten sonra küçük görselleri önceden yükle
            await PreloadSubModelThumbnails(filteredSubModels);
        }
        
        isLoadingSubModels = false;
        StateHasChanged();
    }
    
    private async Task LoadCarBodyOptions()
    {
        if (selectedSubModel == null || selectedBaseModel == null || selectedManufacturer == null || selectedVehicleType == null) return;
        
        isLoadingCarBodyOptions = true;
        StateHasChanged();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        var result = await dotService.GetCarBodyOptionsBySubModelAsync(selectedManufacturer.DatKey, selectedBaseModel.DatKey, selectedSubModel.DatKey, selectedVehicleType.Id);
        if (result.Ok)
        {
            carBodyOptions = result.Result ?? new List<DotCarBodyOption>();
        }
        
        isLoadingCarBodyOptions = false;
        StateHasChanged();
    }
    
    private async Task LoadEngineOptions()
    {
        if (selectedSubModel == null || selectedBaseModel == null || selectedManufacturer == null || selectedVehicleType == null) return;
        
        isLoadingEngineOptions = true;
        StateHasChanged();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        var result = await dotService.GetEngineOptionsBySubModelAsync(selectedManufacturer.DatKey, selectedBaseModel.DatKey, selectedSubModel.DatKey, selectedVehicleType.Id);
        if (result.Ok)
        {
            engineOptions = result.Result ?? new List<DotEngineOption>();
        }
        
        isLoadingEngineOptions = false;
        StateHasChanged();
    }
    
    private async Task LoadOptions()
    {
        if (selectedSubModel == null || selectedBaseModel == null || selectedManufacturer == null || selectedVehicleType == null) return;
        
        isLoadingOptions = true;
        StateHasChanged();
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        var result = await dotService.GetOptionsBySubModelAsync(selectedManufacturer.DatKey, selectedBaseModel.DatKey, selectedSubModel.DatKey, selectedVehicleType.Id);
        if (result.Ok)
        {
            options = result.Result ?? new List<DotOption>();
        }
        
        isLoadingOptions = false;
        StateHasChanged();
    }
    
    private async Task LoadVehicleImages()
    {
        if (selectedSubModel == null || selectedBaseModel == null || selectedManufacturer == null || selectedVehicleType == null) return;
        
        using var scope = _serviceScopeFactory.CreateScope();
        var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
        
        try
        {
            var result = await dotService.GetVehicleImagesByCodesAsync(
                selectedVehicleType.Id.ToString(),
                selectedManufacturer.DatKey,
                selectedBaseModel.DatKey,
                selectedSubModel.DatKey
            );
            
            if (result.Ok && result.Result != null)
            {
                vehicleImages = result.Result;
            }
            else
            {
                vehicleImages = new List<VehicleImageWithCode>();
            }
        }
        catch (Exception ex)
        {
            vehicleImages = new List<VehicleImageWithCode>();
        }
        StateHasChanged();
    }

    private void FilterManufacturers()
    {
        if (string.IsNullOrWhiteSpace(manufacturerSearch))
        {
            filteredManufacturers = manufacturers;
        }
        else
        {
            var searchTerm = manufacturerSearch.Trim();
            filteredManufacturers = manufacturers
                .Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        UpdateManufacturerDisplayList();
        StateHasChanged();
    }

    private IEnumerable<DotManufacturer> OrderManufacturersForDisplay(IEnumerable<DotManufacturer> source)
    {
        if (source == null)
        {
            yield break;
        }
        var popularList = new List<DotManufacturer>();
        var yieldedIds = new HashSet<int>();
        foreach (var m in source)
        {
            if (m == null) { continue; }
            var name = m.Name ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(name) && popularBrandNames.Contains(name.Trim()))
            {
                popularList.Add(m);
            }
        }
        foreach (var m in popularList)
        {
            if (m == null) { continue; }
            yieldedIds.Add(m.Id);
            yield return m;
        }
        foreach (var m in source)
        {
            if (m == null) { continue; }
            if (!yieldedIds.Contains(m.Id))
            {
                yield return m;
            }
        }
    }

    private void UpdateManufacturerDisplayList()
    {
        displayManufacturers = OrderManufacturersForDisplay(filteredManufacturers).ToList();
    }

    private IEnumerable<DotManufacturer> GetBrandListForRender()
    {
        IEnumerable<DotManufacturer> src;
        if (string.IsNullOrWhiteSpace(manufacturerSearch))
        {
            src = manufacturers ?? Enumerable.Empty<DotManufacturer>();
        }
        else
        {
            var searchTerm = manufacturerSearch.Trim();
            src = (manufacturers ?? Enumerable.Empty<DotManufacturer>())
                .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Name) && m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }
        return OrderManufacturersForDisplay(src);
    }

    private void FilterBaseModels()
    {
        if (string.IsNullOrEmpty(baseModelSearch))
        {
            filteredBaseModels = selectedManufacturer != null
                ? baseModels.Where(m => m.ManufacturerKey == selectedManufacturer.DatKey).ToList()
                : baseModels;
        }
        else
        {
            filteredBaseModels = baseModels.Where(m =>
            {
                var rawBaseModelName = m.Name;
                var searchTerm = baseModelSearch.Trim();
                var isMatch = (selectedManufacturer == null || m.ManufacturerKey == selectedManufacturer.DatKey) &&
                              rawBaseModelName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);
                return isMatch;
            }).ToList();
        }
        StateHasChanged();
    }
    
    private void FilterSubModels()
    {
        if (string.IsNullOrEmpty(subModelSearch))
        {
            filteredSubModels = subModels;
           
        }
        else
        {
            filteredSubModels = subModels.Where(sm => 
                sm.Name.Contains(subModelSearch.Trim(), StringComparison.OrdinalIgnoreCase)
            ).ToList();
           
        }
        StateHasChanged();
        // Arka planda görünen liste için küçük görselleri yükle
        _ = PreloadSubModelThumbnails(filteredSubModels);
    }
    
    private async Task SelectVehicleType(DotVehicleType vehicleType)
    {
        selectedVehicleType = vehicleType;
        // Önce 2. adıma geç ve yükleme durumunu kullanıcıya göster
        isLoadingManufacturers = true;
        GoToStep(2);
        StateHasChanged();
        await LoadManufacturers();
    }
    
    private async Task SelectManufacturer(DotManufacturer manufacturer)
    {
        selectedManufacturer = manufacturer;
        baseModelSearch = ""; // Arama filtrelerini sıfırla
        await LoadBaseModels();
        GoToStep(3);
    }
    
    private bool isTransitioningToModelStep = false;

    private async Task SelectBaseModel(DotBaseModel baseModel)
    {
        selectedBaseModel = baseModel;
        subModelSearch = ""; // Arama filtrelerini sıfırla
        // 4. adıma hemen geçip loader'ı göster
        isTransitioningToModelStep = true;
        GoToStep(4);
        StateHasChanged();
        await LoadSubModels();
        // Model seçimi ekranına girerken ilk modeli otomatik seçip görselleri hazırla
        if (filteredSubModels != null && filteredSubModels.Any())
        {
            try
            {
                isLoadingModelThumbs = true;
                StateHasChanged();
                selectedSubModel = filteredSubModels[0];
                vehicleImages = new List<VehicleImageWithCode>();
                await LoadVehicleImages();
            }
            finally
            {
                isLoadingModelThumbs = false;
                StateHasChanged();
            }
        }
        isTransitioningToModelStep = false;
        StateHasChanged();
    }

    private async Task SelectSubModel(DotSubModel subModel)
    {
        selectedSubModel = subModel;
        // 5. adım verilerini hemen yüklemeye başla ve yükleme göstergelerini aç
        isLoadingCarBodyOptions = true;
        isLoadingEngineOptions = true;
        isLoadingOptions = true;
        StateHasChanged();
        _ = LoadCarBodyOptions();
        _ = LoadEngineOptions();
        _ = LoadOptions();

        // Görselleri 4. adımda da güncelleyelim (küçük önizleme için)
        _ = LoadVehicleImages();

        // Otomatik olarak 5. adıma geç (Kasa Tipi)
        GoToStep(5);
    }
    
    private void SelectCarBody(DotCarBodyOption carBody)
    {
        selectedCarBody = carBody;
        GoToStep(6);
    }
    
    private void SelectEngine(DotEngineOption engine)
    {
        selectedEngine = engine;
        GoToStep(7);
    }
    
    private void ToggleOption(DotOption option)
    {
        if (selectedOptions.Any(o => o.Id == option.Id))
        {
            selectedOptions.RemoveAll(o => o.Id == option.Id);
        }
        else
        {
            selectedOptions.Add(option);
        }
        StateHasChanged();
    }
    
    private void GoToStep(int step)
    {
        currentStep = step;
        if (currentStep == 2)
        {
            // 2. adıma her gelişte temel listeyi ve filtreyi güvence altına al
            manufacturers = OrderManufacturersForDisplay(manufacturers).ToList();
            if (string.IsNullOrWhiteSpace(manufacturerSearch))
            {
                filteredManufacturers = manufacturers;
            }
            else
            {
                var term = manufacturerSearch.Trim();
                filteredManufacturers = manufacturers.Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            UpdateManufacturerDisplayList();
        }
        StateHasChanged();
    }
    
    [JSInvokable]
    public void ClearSearch(string searchType)
    {
        switch (searchType)
        {
            case "manufacturer":
                manufacturerSearch = "";
                FilterManufacturers();
                break;
            case "baseModel":
                baseModelSearch = "";
                FilterBaseModels();
                break;
            case "year":
               
                break;
        }
        StateHasChanged();
    }
    
    private bool CanProceedToNextStep()
    {
        return currentStep switch
        {
            1 => selectedVehicleType != null,
            2 => selectedManufacturer != null,
            3 => selectedBaseModel != null,
            4 => selectedSubModel != null,
            5 => selectedCarBody != null,
            6 => selectedEngine != null,
            7 => true,
            8 => !string.IsNullOrEmpty(PlateNumber), // Plaka numarası kontrolü eklendi
            _ => false
        };
    }
    
    private bool CanGoToStep(int step)
    {
        return step switch
        {
            1 => true, // Her zaman ilk adıma gidilebilir
            2 => selectedVehicleType != null,
            3 => selectedVehicleType != null && selectedManufacturer != null,
            4 => selectedVehicleType != null && selectedManufacturer != null && selectedBaseModel != null,
            5 => selectedVehicleType != null && selectedManufacturer != null && selectedBaseModel != null && selectedSubModel != null,
            6 => selectedVehicleType != null && selectedManufacturer != null && selectedBaseModel != null && selectedSubModel != null && selectedCarBody != null,
            7 => selectedVehicleType != null && selectedManufacturer != null && selectedBaseModel != null && selectedSubModel != null && selectedCarBody != null && selectedEngine != null,
            8 => selectedVehicleType != null && selectedManufacturer != null && selectedBaseModel != null && selectedSubModel != null && selectedCarBody != null && selectedEngine != null,
            _ => false
        };
    }
    
    private void GoToStepIfValid(int step)
    {
        if (CanGoToStep(step))
        {
            GoToStep(step);
        }
    }

    private void OnBrandStepClicked()
    {
        if (!CanGoToStep(2)) return;
        // Her tıklamada popüler filtreyi uygula ve UI'ı yenile
        manufacturers = OrderManufacturersForDisplay(manufacturers).ToList();
        if (string.IsNullOrWhiteSpace(manufacturerSearch))
        {
            filteredManufacturers = manufacturers;
        }
        else
        {
            var term = manufacturerSearch.Trim();
            filteredManufacturers = manufacturers.Where(m => !string.IsNullOrWhiteSpace(m.Name) && m.Name.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        UpdateManufacturerDisplayList();
        GoToStep(2);
    }
    
    private bool CanSaveCar()
    {
        var isVehicleTypeSelected = selectedVehicleType != null;
        var isManufacturerSelected = selectedManufacturer != null;
        var isBaseModelSelected = selectedBaseModel != null;
        var isSubModelSelected = selectedSubModel != null;
        var isCarBodySelected = selectedCarBody != null;
        var isEngineSelected = selectedEngine != null;
        var isPlateNumberEntered = !string.IsNullOrEmpty(PlateNumber);


        return isVehicleTypeSelected &&
               isManufacturerSelected && 
               isBaseModelSelected && 
               isSubModelSelected && 
               isCarBodySelected && 
               isEngineSelected &&
               isPlateNumberEntered;
    }
    
    private async Task SaveCar()
    {
        if (!CanSaveCar()) return;
        
        // Global loading başlat
        await _appStateManager.SetGlobalLoading(true);
        
        try
        {
            // UserCars entity'sini oluştur ve kaydet
            var userCar = new UserCars
            {
                Id = EditableCars?.Id ?? 0, // Eğer düzenleme modundaysa Id'yi ata
                UserId = GetCurrentUserId(),
                PlateNumber = PlateNumber, // Plaka numarasını ata
                
                // Dot Integration FK'ları
                DotVehicleTypeId = selectedVehicleType!.Id,
                DotManufacturerId = selectedManufacturer!.Id,
                DotBaseModelId = selectedBaseModel!.Id,
                DotSubModelId = selectedSubModel!.Id,
                DotCarBodyOptionId = selectedCarBody!.Id,
                DotEngineOptionId = selectedEngine!.Id,
                DotOptionId = selectedOptions.FirstOrDefault()?.Id,
                
                // Dot Integration Keys
                DotManufacturerKey = selectedManufacturer.DatKey,
                DotBaseModelKey = selectedBaseModel.DatKey,
                DotSubModelKey = selectedSubModel.DatKey
            };

            var result = await _userCarService.UpsertUserCarAsync(userCar);
            
            // Global loading bitir
            await _appStateManager.SetGlobalLoading(false);
            
            if (result.Ok)
            {
                // Show success notification
                if (IsEditMode)
                {
                    _notificationService.Notify(NotificationSeverity.Success, lang["GarageEditSuccess"]);
                }
                else
                {
                    _notificationService.Notify(NotificationSeverity.Success, lang["GarageAddSuccess"]);
                }
                
                await CloseModal();
            }
            else
            {
                _notificationService.Notify(NotificationSeverity.Error, lang["GarageSaveError"]);
            }
        }
        catch (Exception)
        {
            // Global loading bitir (hata durumunda)
            await _appStateManager.SetGlobalLoading(false);
            throw;
        }
    }
    
    private int GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ecommerceClaimTypes.UserId)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return 0; 
    }
    
    private void ResetForm()
    {
        currentStep = 1;

        selectedVehicleType = null;
        selectedManufacturer = null;
        selectedBaseModel = null;
        selectedSubModel = null;
        selectedCarBody = null;
        selectedEngine = null;
        selectedOptions.Clear();

        _plateNumber = string.Empty;

        manufacturerSearch = "";
        baseModelSearch = "";
        subModelSearch = "";

        // Liste verilerini de sıfırlayabiliriz, ancak LoadInitialData() her açılışta yenilediği için şimdilik gerek yok.
        // manufacturers.Clear();
        // filteredManufacturers.Clear();
        // baseModels.Clear();
        // filteredBaseModels.Clear();
        // subModels.Clear();
        // filteredSubModels.Clear();
        // carBodyOptions.Clear();
        // engineOptions.Clear();
        // options.Clear();

        StateHasChanged();
    }
    

    public async Task CloseModal()
    {
        IsModalVisible = false;
        ResetForm();
        await BlazoredModal.CloseAsync(ModalResult.Ok(true));
    }

    private async Task OnClose()
    {
        IsModalVisible = false;
        ResetForm();
        await BlazoredModal.CloseAsync(ModalResult.Cancel());
    }

    private async Task LoadEditData()
    {
        if (EditableCars == null) return;


        // Ensure initial data like vehicleTypes is loaded (already done in OnInitializedAsync, but a check doesn't hurt)
        if (!vehicleTypes.Any())
        {
            await LoadInitialData(); // Re-load if somehow empty
        }

        // 1. Select Vehicle Type
        selectedVehicleType = vehicleTypes.FirstOrDefault(vt => vt.Id == EditableCars.DotVehicleTypeId);
        

        if (selectedVehicleType != null)
        {
            // 2. Load and Select Manufacturer
            await LoadManufacturers(); // This will load manufacturers based on selectedVehicleType
            await Task.Delay(100); // Small delay to prevent DbContext conflicts
            selectedManufacturer = manufacturers.FirstOrDefault(m => m.Id == EditableCars.DotManufacturerId);
           

            if (selectedManufacturer != null)
            {
                // 3. Load and Select Base Model
                await LoadBaseModels(); // This will load base models based on selectedManufacturer
                await Task.Delay(100); // Small delay to prevent DbContext conflicts
                selectedBaseModel = baseModels.FirstOrDefault(bm => bm.Id == EditableCars.DotBaseModelId);
             

                if (selectedBaseModel != null)
                {
                    // 4. Load and Select Sub Model
                    await LoadSubModels(); // This will load sub models based on selectedBaseModel
                    await Task.Delay(100); // Small delay to prevent DbContext conflicts
                    selectedSubModel = subModels.FirstOrDefault(sm => sm.Id == EditableCars.DotSubModelId);
                   

                    if (selectedSubModel != null)
                    {
                        // 5. Load and Select Car Body Options
                        await LoadCarBodyOptions();
                        await Task.Delay(100); // Small delay to prevent DbContext conflicts
                        selectedCarBody = carBodyOptions.FirstOrDefault(cb => cb.Id == EditableCars.DotCarBodyOptionId);
                     

                        // 6. Load and Select Engine Options
                        await LoadEngineOptions();
                        await Task.Delay(100); // Small delay to prevent DbContext conflicts
                        selectedEngine = engineOptions.FirstOrDefault(en => en.Id == EditableCars.DotEngineOptionId);
                       

                        // 7. Load and Select Options
                        await LoadOptions();
                        await Task.Delay(100); // Small delay to prevent DbContext conflicts
                        if (EditableCars.DotOptionId.HasValue)
                        {
                            selectedOptions = options.Where(o => o.Id == EditableCars.DotOptionId.Value).ToList();
                        }
                        

                        // 8. Load Vehicle Images
                        await LoadVehicleImages();
                        await Task.Delay(100); // Small delay to prevent DbContext conflicts

                        // 9. Set Plate Number
                        PlateNumber = EditableCars.PlateNumber ?? string.Empty;

                        // Set current step to the last one (Plate Number + Images)
                        currentStep = 8;
                    } else { currentStep = 4; }
                } else { currentStep = 3; }
            } else { currentStep = 2; }
        } else { currentStep = 1; }

            StateHasChanged();
    }
    
    
    
    private string GetVehicleTypeIcon(string vehicleTypeName){
        return vehicleTypeIcons.TryGetValue(vehicleTypeName, out var iconClass) ? iconClass : "fas fa-car";
    }
    
    private async Task ViewFullImage(string imageUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("http"))
            {
                Console.WriteLine("Araç görsel URL'i geçersiz veya boş.");
                return;
            }
           
            await _jsRuntime.InvokeVoidAsync("eval", $@"
                var modal = document.createElement('div');
                modal.style.cssText = 'position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.9);z-index:10000;display:flex;align-items:center;justify-content:center;';
                modal.onclick = function() {{ document.body.removeChild(modal); }};
                var img = document.createElement('img');
                img.src = '{imageUrl}';
                img.style.cssText = 'max-width:90%;max-height:90%;object-fit:contain;';
                modal.appendChild(img);
                document.body.appendChild(modal);
            ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Görsel görüntülenirken hata: {ex.Message}");
        }
    }

    private async Task PreloadSubModelThumbnails(IEnumerable<DotSubModel> list, int maxCount = 6)
    {
        if (selectedVehicleType == null || selectedManufacturer == null || selectedBaseModel == null)
        {
            return;
        }

        int count = 0;
        isLoadingModelThumbs = true;
        StateHasChanged();
        foreach (var sm in list)
        {
            if (count >= maxCount) break;
            if (subModelThumbnails.ContainsKey(sm.Id)) { count++; continue; }

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var dotService = scope.ServiceProvider.GetRequiredService<IDotIntegrationService>();
                var result = await dotService.GetVehicleImagesByCodesAsync(
                    selectedVehicleType.Id.ToString(),
                    selectedManufacturer.DatKey,
                    selectedBaseModel.DatKey,
                    sm.DatKey
                );
      
          
                var imgUrl = result.Ok && result.Result != null && result.Result.Any() 
                    ? result.Result[0].Url 
                    : null;
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    subModelThumbnails[sm.Id] = imgUrl;
                    StateHasChanged();
                }
                else
                {
                    Console.WriteLine($"Alt model için görsel URL'i bulunamadı: {sm.Id} - {sm.Name}");
                }
            }
            catch
            {
                // ignore; no image
            }

            count++;
        }
        isLoadingModelThumbs = false;
        StateHasChanged();
    }
}

