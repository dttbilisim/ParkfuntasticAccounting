using ecommerce.EFCore.Context;
using ecommerce.EP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Kurye başvuru belgelerine erişim — presigned URL veya dosya URL'sine yönlendirir.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class CourierApplicationDocumentController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICourierDocumentUrlProvider _urlProvider;
    private readonly ILogger<CourierApplicationDocumentController> _logger;

    public CourierApplicationDocumentController(
        ApplicationDbContext dbContext,
        ICourierDocumentUrlProvider urlProvider,
        ILogger<CourierApplicationDocumentController> logger)
    {
        _dbContext = dbContext;
        _urlProvider = urlProvider;
        _logger = logger;
    }

    /// <summary>
    /// Belge türüne göre başvurudaki dosya yolunu döndürür veya 302 ile URL'ye yönlendirir.
    /// documentType: TaxPlate | SignatureDeclaration | IdCopy | CriminalRecord
    /// </summary>
    [HttpGet("document/{applicationId:int}/{documentType}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDocument(int applicationId, string documentType, CancellationToken ct)
    {
        var application = await _dbContext.Set<ecommerce.Core.Entities.CourierApplication>()
            .AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => new { a.TaxPlatePath, a.SignatureDeclarationPath, a.IdCopyPath, a.CriminalRecordPath })
            .FirstOrDefaultAsync(ct);
        if (application == null)
            return NotFound();

        var path = documentType?.ToLowerInvariant() switch
        {
            "taxplate" => application.TaxPlatePath,
            "signaturedeclaration" => application.SignatureDeclarationPath,
            "idcopy" => application.IdCopyPath,
            "criminalrecord" => application.CriminalRecordPath,
            _ => null
        };

        if (string.IsNullOrEmpty(path))
            return NotFound();

        var url = await _urlProvider.GetDocumentUrlAsync(path, ct);
        if (string.IsNullOrEmpty(url))
            return NotFound();

        return Redirect(url);
    }
}
