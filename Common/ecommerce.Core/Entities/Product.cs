using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
	public class Product: AuditableEntity<int>
    {
        public string Name { get; set; } = null!;
        public string? ShortName { get; set; }
        public string? Description { get; set; }
        public string ? Barcode { get; set; }
        public decimal CartMinValue { get; set; }
        public decimal? CartMaxValue { get; set; }
        public decimal Weight { get; set; }
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public decimal Height { get; set; }
        public decimal CargoDesi {get;set;} = 0;
        public int? BranchId { get; set; }

        public int BrandId { get; set; }
        [ForeignKey("BrandId")]
        public Brand Brand { get; set; } = null!;

        
        public int TaxId { get; set; }
        [ForeignKey("TaxId")]
        public Tax Tax { get; set; } = null!;

        public bool IsNewsProduct{get;set;} = false;
     

        public int? ProductTypeId { get; set; }

        [ForeignKey("ProductTypeId")]
        public ProductType? ProductType { get; set; } = null!;

        /// <summary>PcPos transfer: Ana kategori (tProduct.CatID)</summary>
        public int? CategoryId { get; set; }
        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        public decimal Price{get;set;} = 0;
        public decimal? CostPrice { get; set; }
        public decimal? RetailPrice { get; set; }

        public string ? VideoUrl{get;set;}
        public string? DocumentUrl {get;set;}
        public string ? DocumentUrl2{get;set;}

        public string ? WebKeyword{get;set;}
        public bool IsCustomerCreated{get;set;} = false;

        public bool IsGift{get;set;} = false;
        public bool IsStockFollow { get; set; } = true;

        /// <summary>Web/Plasiyer sipariş ekranında satılsın mı?</summary>
        public bool IsSoldOnWeb { get; set; } = true;
        /// <summary>PcPos'ta satılsın mı?</summary>
        public bool IsSoldOnPcPos { get; set; } = true;

        /// <summary>PcPos transfer: POS durumu</summary>
        public int? StatusPcPos { get; set; }
        /// <summary>PcPos transfer: Satış durumu</summary>
        public int? StatusSales { get; set; }

        /// <summary>Paket ürün mü? İşaretli ise PackageProductIds'deki ürünler paketin içeriğidir.</summary>
        public bool IsPackageProduct { get; set; }
        /// <summary>Paket ürün ise içindeki ürün ID'leri (virgülle ayrılmış, örn: "1,2,3")</summary>
        public string? PackageProductIds { get; set; }
        
        public virtual List<ProductCategories> Categories { get; set; } = null!;
        
        public virtual List<ProductImage> ProductImage { get; set; }
        public virtual List<MyFavorites> MyFavorites { get; set; }
        
        public virtual List<ProductGroupCode> ProductGroupCodes { get; set; }

        public ICollection<ProductTier> ProductTiers { get; set; } = new List<ProductTier>();
        
        public ICollection<ProductUnit> ProductUnits { get; set; } = new List<ProductUnit>();

        /// <summary>PcPos transfer: Ürün-şube eşlemesi</summary>
        public ICollection<ProductBranch> ProductBranches { get; set; } = new List<ProductBranch>();
        /// <summary>PcPos transfer: Paket ürün bileşenleri (bu ürün paket ise)</summary>
        public ICollection<ProductSaleItems> ProductSaleItemsAsRef { get; set; } = new List<ProductSaleItems>();
        /// <summary>PcPos transfer: Bu ürün bir paketin bileşeni ise</summary>
        public ICollection<ProductSaleItems> ProductSaleItemsAsComponent { get; set; } = new List<ProductSaleItems>();
    }
}

