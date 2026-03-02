namespace ecommerce.Domain.Shared.Dtos.OtoIsmail;

public class OtoIsmailCurrencyDto
{
    public string? Ad { get; set; }
    public string? Simge { get; set; }
    public DateTime Tarih { get; set; }
    public decimal Tutar { get; set; }
}

public class OtoIsmailResultCurrencyDto
{
    public OISMLResultDto? Result { get; set; }
    public List<OtoIsmailCurrencyDto>? Data { get; set; }
}
