using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class OemToCars:AuditableEntity<int>{
    public string OEM { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;

    public string Engine { get; set; } = string.Empty;
    public string EngineCode { get; set; } = string.Empty;

    public string Fuel { get; set; } = string.Empty;
    public string Gearbox { get; set; } = string.Empty;

    public string? Body { get; set; }
    public string? Chassis { get; set; }

    public string? VIN { get; set; }

    public string? Position { get; set; }
    public int? MileageKm { get; set; }

    public int? Year { get; set; }
    public string? Years { get; set; }

    public string? ArticleNumber { get; set; }
    public string? OriginalNumbers { get; set; }

    public decimal? Price { get; set; }
    public string? Currency { get; set; }

    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public string ? Decsription{get;set;}
    public string ? Country{get;set;}
}
