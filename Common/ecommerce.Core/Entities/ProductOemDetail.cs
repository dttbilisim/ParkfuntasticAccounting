using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
namespace ecommerce.Core.Entities;


[Table("ProductOemDetail")]
public class ProductOemDetail{
    [Key]
    public int Id{get;set;}
    
    [Required]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Oem { get; set; }

    [Required]
    [MaxLength(500)]
    public string Name { get; set; }
    
    [MaxLength(100)]
    public string DatProcessNumber { get; set; }

    [Column(TypeName = "numeric(18,2)")]
    public decimal? NetPrice { get; set; }

    public int? VehicleType { get; set; }

    [MaxLength(100)]
    public string VehicleTypeName { get; set; }

    public int? ManufacturerKey { get; set; }

    [MaxLength(150)]
    public string ManufacturerName { get; set; }

    public int? BaseModelKey { get; set; }

    [MaxLength(200)]
    public string BaseModelName { get; set; }

   
    [Column(TypeName = "jsonb")]
    public string SubModelsJson { get; set; }

    // JSON deserialize edilmiş hali (veritabanına yazılmaz)
    [NotMapped]
    public List<SubModel> SubModels
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SubModelsJson))
                return new List<SubModel>();

            try
            {
                return JsonSerializer.Deserialize<List<SubModel>>(SubModelsJson);
            }
            catch
            {
                return new List<SubModel>();
            }
        }
        set
        {
            SubModelsJson = JsonSerializer.Serialize(value ?? new List<SubModel>());
        }
    }
    
    
}
