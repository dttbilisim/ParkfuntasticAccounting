using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ecommerce.Core.Entities;
using ecommerce.Domain.Shared.Dtos.SupportLine;

namespace ecommerce.Web.Domain.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IContactEmailService _email;
 

    public EmailTemplateService(IContactEmailService email)
    {
        _email = email;
        
    }

    public async Task SendSupportLineEmailAsync(SupportLine model)
    {
        string? returnTypeDesc = model.SupportLineReturnType?.GetType()
            .GetMember(model.SupportLineReturnType.ToString()!)
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?.Description;

        string? typeDesc = model.SupportLineType.GetType()
            .GetMember(model.SupportLineType.ToString()!)
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?.Description;

        var body = $@"
<strong>Ad Soyad:</strong> {model.FirstName} {model.LastName}<br/>
<strong>E-posta:</strong> {model.Email}<br/>
<strong>Telefon:</strong> {model.PhoneNumber}<br/>
<strong>Talep Tipi:</strong> {typeDesc}<br/>
<strong>Dönüş Şekli:</strong> {returnTypeDesc}<br/>
<strong>Konu:</strong> {model.FrequentlyAskedQuestionsName}<br/>
<strong>Mesaj:</strong><br/>
{model.Description}";

        await _email.SendEmailAsync(
            "naberefe@gmail.com",
            $"{typeDesc} - Yeni Talep",
            body
        );
    }
}
