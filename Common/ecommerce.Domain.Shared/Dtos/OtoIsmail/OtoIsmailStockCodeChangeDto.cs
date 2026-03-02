namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailStockCodeChangeDto
{
    public string? OncekiKod { get; set; }
    public string? SonrakiKod { get; set; }
    public DateTime Tarih { get; set; }
}

public class OtoIsmailResultStockCodeChangeDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailStockCodeChangeDto>? Data { get; set; }
}
