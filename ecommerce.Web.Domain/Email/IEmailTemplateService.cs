using ecommerce.Core.Entities;
using ecommerce.Domain.Shared.Dtos.SupportLine;

namespace ecommerce.Web.Domain.Email;

public interface IEmailTemplateService
{
    Task SendSupportLineEmailAsync(SupportLine model);
}