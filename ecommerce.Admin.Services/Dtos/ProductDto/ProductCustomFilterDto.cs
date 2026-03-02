namespace ecommerce.Admin.Domain.Dtos.ProductDto;
public class ProductCustomFilterDto
{
    public bool? Status { get; set; }
    public bool? ProductsWithoutImage { get; set; }
    public bool? ProductsWithoutCategory { get; set; }
}
