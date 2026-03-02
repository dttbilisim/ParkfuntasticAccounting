namespace ecommerce.Domain.Shared.Options;
public class ElasticSearchOptions
{
    public string Uri { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}