using Nest;
using System.Collections.Generic;
using System.Linq;

namespace ecommerce.Domain.Shared.Dtos.Product;

/// <summary>
/// SellerProduct Elasticsearch DTO (sellerproduct_index)
/// Multi-Index Strategy: BrandId, TaxId, ProductId ile join yapılır
/// </summary>
public class SellerProductElasticDto
{
    // SellerItems fields
    [PropertyName("SellerItemId")]
    public int SellerItemId { get; set; }
    
    [PropertyName("SellerId")]
    public int SellerId { get; set; }

    [PropertyName("SellerName")]
    public string? SellerName { get; set; }
    
    [PropertyName("Stock")]
    public decimal Stock { get; set; }
    
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
    
    [PropertyName("SellerStatus")]
    public int SellerStatus { get; set; }
    
    [PropertyName("SellerModifiedDate")]
    public DateTime? SellerModifiedDate { get; set; }
    
    [PropertyName("SourceId")]
    public string? SourceId { get; set; }
    
    [PropertyName("Step")]
    public double Step { get; set; }
    
    [PropertyName("MinSaleAmount")]
    public double MinSaleAmount { get; set; }
    
    [PropertyName("MaxSaleAmount")]
    public double MaxSaleAmount { get; set; }
    
    // Product fields
    [PropertyName("ProductId")]
    public int ProductId { get; set; }
    
    [PropertyName("ProductName")]
    public string? ProductName { get; set; }
    
    [PropertyName("ProductDescription")]
    public string? ProductDescription { get; set; }
    
    [PropertyName("ProductBarcode")]
    public string? ProductBarcode { get; set; }
    
    [PropertyName("ProductStatus")]
    public int ProductStatus { get; set; }
    
    [PropertyName("DocumentUrl")]
    public string? DocumentUrl { get; set; }
    
    [PropertyName("MainImageUrl")]
    public string? MainImageUrl { get; set; }
    
    // JOIN Keys (multi-index strategy)
    [PropertyName("BrandId")]
    public int? BrandId { get; set; }
    
    [PropertyName("CategoryId")]
    public int? CategoryId { get; set; }
    
    [PropertyName("TaxId")]
    public int? TaxId { get; set; }
    
    // DotParts fields (LEFT JOIN - NULL olabilir, sadece İLK GroupCode)
    [PropertyName("PartNumber")]
    public string? PartNumber { get; set; }
    
    [PropertyName("DotPartName")]
    public string? DotPartName { get; set; }
    
    [PropertyName("ManufacturerName")]
    public string? ManufacturerName { get; set; }
    
    [PropertyName("ProductBrandName")]
    public string? ProductBrandName { get; set; }
    
    [PropertyName("VehicleTypeName")]
    public string? VehicleTypeName { get; set; }
    
    [PropertyName("DotPartDescription")]
    public string? DotPartDescription { get; set; }
    
    [PropertyName("BaseModelName")]
    public string? BaseModelName { get; set; }
    
    [PropertyName("NetPrice")]
    public double? NetPrice { get; set; }
    
    [PropertyName("PriceDate")]
    public DateTime? PriceDate { get; set; }
    
    [PropertyName("DatProcessNumber")]
    public List<string>? DatProcessNumber { get; set; }
    
    [PropertyName("VehicleType")]
    public int? VehicleType { get; set; }
    
    [PropertyName("ManufacturerKey")]
    public string? ManufacturerKey { get; set; }
    
    [PropertyName("BaseModelKey")]
    public string? BaseModelKey { get; set; }
    
    [PropertyName("OemCode")]
    public List<string>? OemCode { get; set; }
    
    [PropertyName("SubModelsJson")]
    public List<SubModelDto>? SubModelsJson { get; set; }

    [PropertyName("Parts")]
    public List<ProductPartDto>? Parts { get; set; }

    [PropertyName("GroupCode")]
    public string? GroupCode { get; set; }
}

/// <summary>
/// ProductGroupCode Index DTO (Ayrı index - opsiyonel)
/// DotParts bilgisi gerekirse bu index'ten çekilir
/// </summary>
public class ProductGroupCodeIndexDto
{
    [PropertyName("ProductId")]
    public int ProductId { get; set; }
    
    [PropertyName("OemCode")]
    public string? OemCode { get; set; }
    
    // DotParts fields
    [PropertyName("PartNumber")]
    public string? PartNumber { get; set; }
    
    [PropertyName("DotPartName")]
    public string? DotPartName { get; set; }
    
    [PropertyName("ManufacturerName")]
    public string? ManufacturerName { get; set; }
    
    [PropertyName("VehicleTypeName")]
    public string? VehicleTypeName { get; set; }
    
    [PropertyName("BaseModelName")]
    public string? BaseModelName { get; set; }
    
    [PropertyName("NetPrice")]
    public double? NetPrice { get; set; }
}

/// <summary>
/// ViewModel (C# tarafında joined data)
/// </summary>
public class SellerProductViewModel
{
    // SellerProduct base data
    public int SellerItemId { get; set; }
    public int ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public string? ProductBarcode { get; set; }
    public string? DocumentUrl { get; set; }
    public string? MainImageUrl { get; set; }
    public int Stock { get; set; }
    public double SalePrice { get; set; }
    public double CostPrice { get; set; }
    public string? Currency { get; set; }
    public string? Unit { get; set; }
    public int SellerId { get; set; }
    public string? SellerName { get; set; }
    public DateTime? SellerModifiedDate { get; set; }
    public string? SourceId { get; set; }
    public double Step { get; set; }
    public double MinSaleAmount { get; set; }
    public double MaxSaleAmount { get; set; }
    
    // DotParts data (null olabilir, sadece İLK GroupCode)
    public string? PartNumber { get; set; }
    public string? DotPartName { get; set; }
    public string? ManufacturerName { get; set; }
    public string? VehicleTypeName { get; set; }
    public string? DotPartDescription { get; set; }
    public string? BaseModelName { get; set; }
    public double? NetPrice { get; set; }
    public DateTime? PriceDate { get; set; }
    public List<string>? DatProcessNumber { get; set; }
    public int? VehicleType { get; set; }
    public string? ManufacturerKey { get; set; }
    public string? BaseModelKey { get; set; }
    public List<string>? OemCode { get; set; }
    public int SimilarProductCount { get; set; }
    public bool IsEquivalent { get; set; }
    
    /// <summary>Paket ürün mü? Paket ürünlerde PackageDetailModal açılır.</summary>
    public bool IsPackageProduct { get; set; }
    
    // Helper property for backward compatibility (first OemCode or empty)
    public string? OemCodeFirst => OemCode?.FirstOrDefault();
    public List<SubModelDto>? SubModelsJson { get; set; }
    
    // Joined from other indices
    public BrandDto? Brand { get; set; }
    public TaxDto? Tax { get; set; }
    public List<CategoryDto>? Categories { get; set; }
    public List<ProductImageDto>? Images { get; set; }
    public List<ProductPartDto>? Parts { get; set; }
    public List<SellerProductCompatibilityDto>? PerfectCompatibilityCars { get; set; }
    public string? GroupCode => PartNumber;
    
    // ML Field Tracking: Which Elasticsearch fields matched this product
    public List<string>? MatchedFields { get; set; }
    public Dictionary<string, double>? FieldScores { get; set; }
}


public class SellerProductCompatibilityDto
{
    public int CarId { get; set; }
    public string? PlateNumber { get; set; }
    public string? ManufacturerName { get; set; }
    public string? BaseModelName { get; set; }
    public string? SubModelName { get; set; }
    public string? ManufacturerKey { get; set; }
    public string? BaseModelKey { get; set; }
    public string? SubModelKey { get; set; }
}

/// <summary>
/// Image Index DTO (image_index)
/// </summary>
public class ImageIndexDto
{
    [PropertyName("Id")]
    public int Id { get; set; }
    
    [PropertyName("ProductId")]
    public int ProductId { get; set; }
    
    [PropertyName("FileName")]
    public string? FileName { get; set; }
    
    [PropertyName("FileGuid")]
    public string? FileGuid { get; set; }
    
    [PropertyName("CreatedDate")]
    public DateTime? CreatedDate { get; set; }
    
    [PropertyName("ModifiedDate")]
    public DateTime? ModifiedDate { get; set; }
}

