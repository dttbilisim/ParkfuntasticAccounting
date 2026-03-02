using System.ComponentModel.DataAnnotations;
namespace ecommerce.Core.Entities;
public class DatData{
    
    [Key]
    public int Id{get;set;}
    public int VehicleTypeKey{get;set;}
    public int ManufactureKey{get;set;}
    public int BaseModelKey{get;set;}
    public int DatProcessNo{get;set;}
    public string ? Name{get;set;}
    public int ET{get;set;}
    
    /// <summary>
    /// Bu DatProcessNo'nun parçaları DotParts'a aktarıldı mı?
    /// false = Henüz aktarılmadı, true = Aktarıldı
    /// </summary>
    public bool IsTrans{get;set;} = false;
    
}
