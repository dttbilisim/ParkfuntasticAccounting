# DAT Integration Library

Bu kütüphane [DAT (Deutscher Automobil Treuhand)](https://www.dat.de/) SilverDAT API'si ile entegrasyon sağlar.

## Özellikler

- ✅ SOAP Authentication
- ✅ Token Cache (otomatik yenileme)
- ✅ Vehicle Types API
- ✅ Vehicle Details API  
- ✅ Parts Search API
- ✅ Part Details API
- ✅ Error Handling & Logging
- ✅ Dependency Injection Support

## Kurulum

### 1. Proje Referansı Ekle
```xml
<ProjectReference Include="Dot.Integration.csproj" />
```

### 2. Service Registration
```csharp
// Program.cs veya Startup.cs
builder.Services.AddDatIntegration(builder.Configuration);
```

### 3. Configuration
```json
{
  "DatService": {
    "AuthenticationUrl": "https://api.dat.de/services/Authentication",
    "VehicleServiceUrl": "https://api.dat.de/services/VehicleSelectionService", 
    "PartsServiceUrl": "https://api.dat.de/services/PartsService",
    "CustomerNumber": "YOUR_CUSTOMER_NUMBER",
    "CustomerLogin": "YOUR_CUSTOMER_LOGIN",
    "CustomerPassword": "YOUR_CUSTOMER_PASSWORD",
    "InterfacePartnerNumber": "YOUR_INTERFACE_PARTNER_NUMBER",
    "InterfacePartnerSignature": "YOUR_INTERFACE_PARTNER_SIGNATURE",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3
  }
}
```

## Kullanım

### Cascade Senkronizasyon (Önerilen Kullanım)

`DatVehicleSyncService` ile iç içe tüm araç bilgilerini tek seferde çekebilirsiniz:

```csharp
// Inject the sync service
private readonly DatVehicleSyncService _syncService;

// 1. Tek bir araç türü için TÜM bilgileri senkronize et
// (Manufacturers -> BaseModels -> SubModels -> Options)
await _syncService.SyncCompleteVehicleDataAsync(vehicleType: 1);

// 2. Belirli bir manufacturer için senkronize et
await _syncService.SyncManufacturerDataAsync(
    vehicleType: 4, 
    manufacturerKey: "750" // Mercedes-Benz
);

// 3. Belirli bir base model için senkronize et
await _syncService.SyncBaseModelDataAsync(
    vehicleType: 4, 
    manufacturerKey: "750", 
    baseModelKey: "11"
);

// 4. Belirli bir sub model için opsiyonları senkronize et
await _syncService.SyncSubModelDataAsync(
    vehicleType: 4, 
    manufacturerKey: "750", 
    baseModelKey: "11",
    subModelKey: "1"
);

// 5. DAT E-Code oluştur ve kaydet
var datECode = await _syncService.CompileAndSaveDatECodeAsync(
    vehicleType: 1,
    manufacturerKey: "130",
    baseModelKey: "82",
    subModelKey: "15",
    selectedOptions: new List<string> { "10003", "83950", "75205" }
);
// Result: "011300820150001"

// 6. TÜM araç türleri için senkronizasyon (DİKKAT: ÇOK UZUN SÜREBİLİR!)
await _syncService.SyncAllVehicleTypesAsync();
```

### Manuel Servis Kullanımı

### Vehicle Types
```csharp
public class MyService
{
    private readonly IDatService _datService;
    
    public MyService(IDatService datService)
    {
        _datService = datService;
    }
    
    public async Task<List<DatVehicleType>> GetVehicleTypes()
    {
        var result = await _datService.GetVehicleTypesAsync();
        return result.VehicleTypes.VehicleType;
    }
}
```

### Vehicle Details
```csharp
public async Task<List<DatVehicle>> GetVehicleDetails(string vehicleTypeId)
{
    var result = await _datService.GetVehicleDetailsAsync(vehicleTypeId);
    return result.Vehicles.Vehicle;
}
```

### Parts Search
```csharp
public async Task<List<DatPart>> SearchParts(string vehicleId, string? partNumber = null)
{
    var result = await _datService.SearchPartsAsync(vehicleId, partNumber);
    return result.Parts.Part;
}
```

### Part Details
```csharp
public async Task<List<DatPart>> GetPartDetails(string partNumber)
{
    var result = await _datService.GetPartDetailsAsync(partNumber);
    return result.Parts.Part;
}
```

## API Endpoints

DAT API'si aşağıdaki servisleri sağlar:

- **Authentication**: Token alma ve cache
- **VehicleSelectionService**: Araç türleri ve detayları
- **PartsService**: Parça arama ve detayları

## Error Handling

Tüm servisler try-catch ile sarılmıştır ve hatalar loglanır. Token cache mekanizması sayesinde gereksiz authentication istekleri önlenir.

## Logging

Microsoft.Extensions.Logging kullanılır. Tüm API çağrıları ve hatalar loglanır.

## Thread Safety

Token cache SemaphoreSlim ile thread-safe'dir. Aynı anda birden fazla token isteği yapılmaz.
