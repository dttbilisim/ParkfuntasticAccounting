namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailVehicleBrandDto
{
    public string? Marka { get; set; }
}

public class OtoIsmailResultVehicleBrandDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailVehicleBrandDto>? Data { get; set; }
}

public class OtoIsmailVehicleProductDto
{
    public int NetsisStokId { get; set; }
    public string? Model1 { get; set; }
    public string? Model2 { get; set; }
    public string? Model3 { get; set; }
    public string? Model4 { get; set; }
    public string? Model5 { get; set; }
    public string? Model6 { get; set; }
    public string? Model7 { get; set; }
    public string? Model8 { get; set; }
    public string? Model9 { get; set; }
    public string? Model10 { get; set; }
    public string? Model11 { get; set; }
    public string? Model12 { get; set; }
    public string? Model13 { get; set; }
    public string? Model14 { get; set; }
    public string? Model15 { get; set; }
    public string? Model16 { get; set; }
    public string? Model17 { get; set; }
    public string? Model18 { get; set; }
    public string? Model19 { get; set; }
    public string? Model20 { get; set; }
    public string? Model21 { get; set; }
}

public class OtoIsmailResultVehicleProductDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailVehicleProductDto>? Data { get; set; }
}
