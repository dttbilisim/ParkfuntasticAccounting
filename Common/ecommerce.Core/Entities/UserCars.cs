using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class UserCars:AuditableEntity<int>{
    
    // Web projesi için eski FK (User tablosu)
    [ForeignKey("UserId")] public User User{get;set;}
    public int UserId{get;set;}
    
    // Mobil API için yeni FK (ApplicationUser tablosu)
    [ForeignKey("ApplicationUserId")] public ApplicationUser? ApplicationUser{get;set;}
    public int? ApplicationUserId{get;set;}
   
    
    
    // Dot Integration Foreign Keys
    [ForeignKey("DotVehicleTypeId")] public DotVehicleType? DotVehicleType { get; set; }
    public int? DotVehicleTypeId { get; set; }
    
    [ForeignKey("DotManufacturerId")] public DotManufacturer? DotManufacturer { get; set; }
    public int? DotManufacturerId { get; set; }
    
    [ForeignKey("DotBaseModelId")] public DotBaseModel? DotBaseModel { get; set; }
    public int? DotBaseModelId { get; set; }
    
    [ForeignKey("DotSubModelId")] public DotSubModel? DotSubModel { get; set; }
    public int? DotSubModelId { get; set; }
    
    [ForeignKey("DotCarBodyOptionId")] public DotCarBodyOption? DotCarBodyOption { get; set; }
    public int? DotCarBodyOptionId { get; set; }
    
    [ForeignKey("DotEngineOptionId")] public DotEngineOption? DotEngineOption { get; set; }
    public int? DotEngineOptionId { get; set; }
    
    [ForeignKey("DotOptionId")] public DotOption? DotOption { get; set; }
    public int? DotOptionId { get; set; }
    
    [ForeignKey("DotCompiledCodeId")] public DotCompiledCode? DotCompiledCode { get; set; }
    public int? DotCompiledCodeId { get; set; }
    
    // Dot Integration Keys (for searching)
    public string? DotManufacturerKey { get; set; }
    public string? DotBaseModelKey { get; set; }
    public string? DotSubModelKey { get; set; }
    public string? DotDatECode { get; set; }


    public string? PlateNumber { get; set; }
    
}
