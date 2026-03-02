using ecommerce.Core.Helpers;

namespace ecommerce.EP.Services;

public class CourierDocumentUploadService : ICourierDocumentUploadService
{
    private const string Folder = "CourierDocuments";
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".heic" };

    private readonly FileHelper _fileHelper;
    private readonly ILogger<CourierDocumentUploadService> _logger;

    public CourierDocumentUploadService(FileHelper fileHelper, ILogger<CourierDocumentUploadService> logger)
    {
        _fileHelper = fileHelper;
        _logger = logger;
    }

    public async Task<string?> SaveAsync(IFormFile? file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            return null;

        if (file.Length > MaxFileSize)
        {
            _logger.LogWarning("Courier document too large: {Size} bytes", file.Length);
            return null;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            _logger.LogWarning("Courier document invalid extension: {Name}", file.FileName);
            return null;
        }

        var uploadPath = _fileHelper.GetUploadPath();
        var folderPath = Path.Combine(uploadPath, Folder);
        Directory.CreateDirectory(folderPath);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folderPath, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        return Path.Combine(Folder, fileName).Replace('\\', '/');
    }
}
