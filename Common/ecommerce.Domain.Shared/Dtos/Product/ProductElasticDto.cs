using Nest;

namespace ecommerce.Domain.Shared.Dtos.Product;

public class ProductElasticDto
{
    [PropertyName("Id")]
    public int? Id { get; set; }

    [PropertyName("Name")]
    public string? Name { get; set; }

    [PropertyName("ShortName")]
    public string? ShortName { get; set; }

    [PropertyName("Description")]
    public string? Description { get; set; }

    [PropertyName("Barcode")]
    public string? Barcode { get; set; }

    [PropertyName("CartMinValue")]
    public decimal? CartMinValue { get; set; }

    [PropertyName("CartMaxValue")]
    public decimal? CartMaxValue { get; set; }

    [PropertyName("Weight")]
    public decimal? Weight { get; set; }

    [PropertyName("Width")]
    public decimal? Width { get; set; }

    [PropertyName("Length")]
    public decimal? Length { get; set; }

    [PropertyName("Height")]
    public decimal? Height { get; set; }

    [PropertyName("BrandId")]
    public int? BrandId { get; set; }

    [PropertyName("TaxId")]
    public int? TaxId { get; set; }

    [PropertyName("Price")]
    public decimal? Price { get; set; }

    [PropertyName("CostPrice")]
    public decimal? CostPrice { get; set; }

    [PropertyName("RetailPrice")]
    public decimal? RetailPrice { get; set; }

    [PropertyName("Status")]
    public int? Status { get; set; }

    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }

    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }

    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }

    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }

    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }

    [PropertyName("ProductTypeId")]
    public int? ProductTypeId { get; set; }

    [PropertyName("CargoDesi")]
    public decimal? CargoDesi { get; set; }

    [PropertyName("IsNewsProduct")]
    public bool? IsNewsProduct { get; set; }

    [PropertyName("DocumentUrl")]
    public string? DocumentUrl { get; set; }

    [PropertyName("DocumentUrl2")]
    public string? DocumentUrl2 { get; set; }

    [PropertyName("VideoUrl")]
    public string? VideoUrl { get; set; }

    [PropertyName("WebKeyword")]
    public string? WebKeyword { get; set; }

    [PropertyName("IsCustomerCreated")]
    public bool? IsCustomerCreated { get; set; }

    [PropertyName("AdvertCount")]
    public int? AdvertCount { get; set; }

    [PropertyName("AvgPrice")]
    public decimal? AvgPrice { get; set; }

    [PropertyName("MaxPrice")]
    public decimal? MaxPrice { get; set; }

    [PropertyName("MinPrice")]
    public decimal? MinPrice { get; set; }

    [PropertyName("IsGift")]
    public bool? IsGift { get; set; }

    [PropertyName("SellerId")]
    public int? SellerId { get; set; }

    [PropertyName("Categories")]
    public List<CategoryDto>? Categories { get; set; }

    [PropertyName("Images")]
    public List<ProductImageDto>? Images { get; set; }

    [PropertyName("Brand")]
    public BrandDto? Brand { get; set; }

    [PropertyName("Tax")]
    public TaxDto? Tax { get; set; }

    [PropertyName("GroupCodes")]
    public List<ProductGroupCodeDto>? GroupCodes { get; set; }
    
    [PropertyName("Favorites")]
    public List<FavoriteElasticDto> Favorites { get; set; }
    
    [PropertyName("SellerItems")]
    public List<SellerItemDto>? SellerItems { get; set; }
    
    [PropertyName("Parts")]
    public List<ProductPartDto>? Parts { get; set; }
    
    [PropertyName("CompatibleVehicles")]
    public List<CompatibleVehicleDto>? CompatibleVehicles { get; set; }
}
public class CategoryDto
{
    [PropertyName("Id")]
    public int? Id { get; set; }

    [PropertyName("Name")]
    public string? Name { get; set; }

    [PropertyName("ParentId")]
    public int? ParentId { get; set; }

    [PropertyName("Status")]
    public int? Status { get; set; }

    [PropertyName("Order")]
    public int? Order { get; set; }

    [PropertyName("ImageUrl")]
    public string? ImageUrl { get; set; }

    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }

    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }

    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }

    [PropertyName("IsMainPage")]
    public bool? IsMainPage { get; set; }

    [PropertyName("IsMainSlider")]
    public bool? IsMainSlider { get; set; }

    [PropertyName("Height")]
    public int? Height { get; set; }

    [PropertyName("SubCategoryCount")]
    public int? SubCategoryCount { get; set; }

    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }

    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }
}

public class ProductImageDto
{
    [PropertyName("Id")]
    public int? Id { get; set; }
    [PropertyName("ProductId")]
    public int? ProductId { get; set; }
    [PropertyName("Order")]
    public int? Order { get; set; }
    [PropertyName("FileGuid")]
    public string? FileGuid { get; set; }
    [PropertyName("FileName")]
    public string? FileName { get; set; }
    [PropertyName("Root")]
    public string? Root { get; set; }
    [PropertyName("Status")]
    public int? Status { get; set; }
    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }
    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }
    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }
    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }
    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }
    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }
}

public class BrandDto
{
    [PropertyName("Id")]
    public int? Id { get; set; }
    [PropertyName("Name")]
    public string? Name { get; set; }
    [PropertyName("Status")]
    public int? Status { get; set; }
    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }
    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }
    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }
    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }
    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }
    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }
}

public class TaxDto
{
    [PropertyName("Id")]
    public int? Id { get; set; }
    [PropertyName("Name")]
    public string? Name { get; set; }
    [PropertyName("TaxRate")]
    public decimal? TaxRate { get; set; }
    [PropertyName("Status")]
    public int? Status { get; set; }
    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }
    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }
    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }
    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }
    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }
    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }
}

public class ProductGroupCodeDto
{
    [PropertyName("ProductId")]
    public int? ProductId { get; set; }

    [PropertyName("Id")]
    public int? Id { get; set; }

    [PropertyName("GroupCode")]
    public string? GroupCode { get; set; }

    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }

    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }

    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }

    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }

    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }
}

public class SellerItemDto
{
    [PropertyName("Id")]
    public int Id { get; set; }
    
    [PropertyName("SellerId")]
    public int SellerId { get; set; }
    
    [PropertyName("Stock")]
    public int Stock { get; set; }
    
    [PropertyName("CostPrice")]
    public double CostPrice { get; set; }
    
    [PropertyName("SalePrice")]
    public double SalePrice { get; set; }
    
    [PropertyName("Commision")]
    public double Commision { get; set; }
    
    [PropertyName("Currency")]
    public string? Currency { get; set; }
    
    [PropertyName("Unit")]
    public string? Unit { get; set; }
    
    [PropertyName("Status")]
    public int Status { get; set; }
    
    [PropertyName("CreatedId")]
    public int? CreatedId { get; set; }
    
    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }
    
    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }
    
    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }
}
public class FavoriteElasticDto
{
    [PropertyName("Id")]
    public int Id { get; set; }

    [PropertyName("ProductId")]
    public int ProductId { get; set; }

    [PropertyName("UserId")]
    public int UserId { get; set; }

    [PropertyName("Status")]
    public int Status { get; set; }

    [PropertyName("CreatedDate")]
    public DateTime CreatedDate { get; set; }

    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }

    [PropertyName("ModifiedId")]
    public int? ModifiedId { get; set; }

    [PropertyName("DeletedDate")]
    public DateTime? DeletedDate { get; set; }

    [PropertyName("DeletedId")]
    public int? DeletedId { get; set; }
}

public class ProductPartDto
{
    [PropertyName("Oem")]
    public string? Oem { get; set; }
    
    [PropertyName("Name")]
    public string? Name { get; set; }
    
    [PropertyName("DatProcessNumber")]
    public List<string>? DatProcessNumber { get; set; }
    
    [PropertyName("NetPrice")]
    public decimal? NetPrice { get; set; }
    
    [PropertyName("VehicleType")]
    public int? VehicleType { get; set; }
    
    [PropertyName("VehicleTypeName")]
    public string? VehicleTypeName { get; set; }
    
    [PropertyName("ManufacturerKey")]
    public int? ManufacturerKey { get; set; }
    
    [PropertyName("ManufacturerName")]
    public string? ManufacturerName { get; set; }
    
    [PropertyName("BaseModelKey")]
    public int? BaseModelKey { get; set; }
    
    [PropertyName("BaseModelName")]
    public string? BaseModelName { get; set; }
    
    [PropertyName("SubModelsJson")]
    public List<SubModelDto>? SubModelsJson { get; set; }
}

public class SubModelDto
{
    [PropertyName("Key")]
    public string? Key { get; set; }
    
    [PropertyName("Name")]
    public string? Name { get; set; }
}

public class CompatibleVehicleDto
{
    [PropertyName("DatECode")]
    public string? DatECode { get; set; }
    
    [PropertyName("VehicleTypeName")]
    public string? VehicleTypeName { get; set; }
    
    [PropertyName("ManufacturerName")]
    public string? ManufacturerName { get; set; }
    
    [PropertyName("BaseModelName")]
    public string? BaseModelName { get; set; }
    
    [PropertyName("SubModelName")]
    public string? SubModelName { get; set; }
    
    [PropertyName("Equipment")]
    public List<VehicleEquipmentDto>? Equipment { get; set; }
    
    [PropertyName("SpareParts")]
    public List<VehicleSparePartDto>? SpareParts { get; set; }
}

public class VehicleEquipmentDto
{
    [PropertyName("DatEquipmentId")]
    public string? DatEquipmentId { get; set; }
    
    [PropertyName("Description")]
    public string? Description { get; set; }
}

public class VehicleSparePartDto
{
    [PropertyName("PartNumber")]
    public string? PartNumber { get; set; }
    
    [PropertyName("Description")]
    public string? Description { get; set; }
    
    [PropertyName("LastUPE")]
    public decimal? LastUPE { get; set; }
    
    [PropertyName("LastUPEDate")]
    public DateTime? LastUPEDate { get; set; }
    
    [PropertyName("Orderable")]
    public bool? Orderable { get; set; }
}