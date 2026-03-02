namespace Dot.Integration.Options;

public class DatServiceOptions
{
    public string AuthenticationUrl { get; set; } = string.Empty;
    public string VehicleServiceUrl { get; set; } = string.Empty;
    public string PartsServiceUrl { get; set; } = string.Empty;
    public string VehicleRepairServiceUrl { get; set; } = string.Empty;
    public string VehicleImageryServiceUrl { get; set; } = string.Empty;
    public string ConversionFunctionsServiceUrl { get; set; } = string.Empty;
    public string CustomerNumber { get; set; } = string.Empty;
    public string CustomerLogin { get; set; } = string.Empty;
    public string CustomerPassword { get; set; } = string.Empty;
    public string InterfacePartnerNumber { get; set; } = string.Empty;
    public string InterfacePartnerSignature { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;

    // Locale and restriction defaults
    public string LocaleCountry { get; set; } = "tr";
    public string DatCountryIndicator { get; set; } = "tr";
    public string Language { get; set; } = "tr";
    public string DefaultRestriction { get; set; } = "APPRAISAL"; // or APPRAISAL
    public bool EnableProcessedDataCheck { get; set; } = true;
}