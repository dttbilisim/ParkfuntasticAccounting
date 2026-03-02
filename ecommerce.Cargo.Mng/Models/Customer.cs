namespace ecommerce.Cargo.Mng.Models;

public class Customer
{
    public long? CustomerId { get; set; }
    
    public string RefCustomerId { get; set; } = null!;
    
    public int? CityCode { get; set; }

    public string CityName { get; set; } = null!;
    
    public int? DistrictCode { get; set; }

    public string DistrictName { get; set; } = null!;
    
    public string Address { get; set; } = null!;
    
    public string? BussinessPhoneNumber { get; set; }
    
    public string Email { get; set; } = null!;
    
    public string? TaxOffice { get; set; }
    
    public string? TaxNumber { get; set; }
    
    public string FullName { get; set; } = null!;
    
    public string? HomePhoneNumber { get; set; }
    
    public string? MobilePhoneNumber { get; set; }
    
}