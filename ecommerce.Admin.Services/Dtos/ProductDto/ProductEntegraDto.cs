using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ProductDto;
public class ProductEntegraDto{
    
    public class Brand
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Category
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class CategoryId
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Description
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class MainCategory
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class MainCategoryId
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Name
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Product
    {
        public ProductCode Product_code { get; set; }
        public string ? Product_id { get; set; }
        public string Barcode { get; set; }
        public Name Name { get; set; }
        public MainCategory mainCategory { get; set; }
        public MainCategoryId mainCategory_id { get; set; }
        public Category category { get; set; }
        public CategoryId category_id { get; set; }
        public SubCategory subCategory { get; set; }
        public SubCategoryId subCategory_id { get; set; }
        public DateTime? Miad { get; set; }
        public decimal Price { get; set; }
        public string CurrencyType { get; set; }
        public int? Tax { get; set; }
        public int Stock { get; set; }
        public Brand Brand { get; set; }
        public string ? Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public string Image4 { get; set; }
        public string Image5 { get; set; }
        public decimal? agirlik{get;set;}
        public decimal? width { get; set; }
        public decimal? height { get; set; }
        public decimal? desi {get;set;}
        public Description Description { get; set; }
    }

    public class ProductCode
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Products
    {
        public List<Product> Product { get; set; }
    }

    public class Root
    {
        [JsonProperty("?xml")]
        public Xml xml { get; set; }
        public Products Products { get; set; }
    }

    public class SubCategory
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class SubCategoryId
    {
        [JsonProperty("#cdata-section")]
        public string cdatasection { get; set; }
    }

    public class Xml
    {
        [JsonProperty("@version")]
        public string version { get; set; }
    }
}
