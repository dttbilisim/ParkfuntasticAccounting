using ecommerce.Core.Helpers;

namespace ecommerce.EP.Services;

/// <summary>
/// Mevcut dosya depolama için belge URL'si: FileHelper.GetFileUrl kullanır.
/// </summary>
public class FileHelperCourierDocumentUrlProvider : ICourierDocumentUrlProvider
{
    private readonly FileHelper _fileHelper;

    public FileHelperCourierDocumentUrlProvider(FileHelper fileHelper)
    {
        _fileHelper = fileHelper;
    }

    public Task<string?> GetDocumentUrlAsync(string? path, CancellationToken ct = default)
    {
        return Task.FromResult(_fileHelper.GetFileUrl(path));
    }
}
