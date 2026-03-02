using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Domain.Shared.Services;
public interface IEmailTemplateService{

    public Task<IActionResult<EmailTemplates>> GetEmailTemplate(EmailTemplateType type);
}
