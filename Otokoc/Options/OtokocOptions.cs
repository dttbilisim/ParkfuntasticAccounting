namespace Otokoc.Options;
public class OtokocOptions{
    public const string SectionName = "Otokoc";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string ImageSavePath { get; set; } = string.Empty;
}