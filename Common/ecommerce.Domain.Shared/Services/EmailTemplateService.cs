using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Domain.Shared.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    public EmailTemplateService(IUnitOfWork<ApplicationDbContext> context)
    {
        _context = context;
    }

    public async Task<IActionResult<EmailTemplates>> GetEmailTemplate(EmailTemplateType type)
    {
        var rs = new IActionResult<EmailTemplates>();
        try
        {
            var data = await _context.DbContext.EmailTemplates
                .FirstOrDefaultAsync(x => x.EmailTemplateType == type && x.Status == (int)EntityStatus.Active);

            if (data == null)
            {
                rs.AddError($"E-posta şablonu bulunamadı: {type}");
                return rs;
            }

            rs.Result = data;
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddError("Şablon alınırken bir hata oluştu.");
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}
