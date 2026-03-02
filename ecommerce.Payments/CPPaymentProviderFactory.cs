using System.Text;
using ecommerce.Core.Utils;
using ecommerce.Payments.Providers;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Payments;

public class CPPaymentProviderFactory : IPaymentProviderFactory
{
    private static readonly Dictionary<BankNames, Type> _providerTypes = new Dictionary<BankNames, Type>
    {
        // NestPay Banks -> Use NEW CPNestPayProvider
        { BankNames.AkBank, typeof(CPNestPayProvider) },
        { BankNames.IsBankasi, typeof(CPNestPayProvider) },
        { BankNames.HalkBank, typeof(CPNestPayProvider) },
        { BankNames.ZiraatBankasi, typeof(CPNestPayProvider) },
        { BankNames.TurkEkonomiBankasi, typeof(CPNestPayProvider) },
        { BankNames.IngBank, typeof(CPNestPayProvider) },
        { BankNames.TurkiyeFinans, typeof(CPNestPayProvider) },
        { BankNames.AnadoluBank, typeof(CPNestPayProvider) },
        { BankNames.HSBC, typeof(CPNestPayProvider) },
        { BankNames.SekerBank, typeof(CPNestPayProvider) },

        // Other Banks -> Retain Original Providers from ecommerce.Virtual.Pos
        { BankNames.DenizBank, typeof(DenizbankPaymentProvider) },
        { BankNames.FinansBank, typeof(FinansbankPaymentProvider) },
        { BankNames.Garanti, typeof(GarantiPaymentProvider) },
        { BankNames.KuveytTurk, typeof(KuveytTurkPaymentProvider) },
        { BankNames.VakifBank, typeof(VakifbankPaymentProvider) },
        { BankNames.Yapikredi, typeof(PosnetPaymentProvider) },
        { BankNames.Albaraka, typeof(PosnetPaymentProvider) },
    };

    private readonly IServiceProvider _serviceProvider;

    public CPPaymentProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPaymentProvider Create(BankNames bankName)
    {
        if (!_providerTypes.ContainsKey(bankName))
            throw new NotSupportedException($"Bank '{bankName}' not supported in CPPaymentProviderFactory");

        Type type = _providerTypes[bankName];
        return ActivatorUtilities.CreateInstance(_serviceProvider, type) as IPaymentProvider;
    }

    public string CreatePaymentFormHtml(IDictionary<string, object> parameters, Uri actionUrl, bool appendSubmitScript = true)
    {
        if (parameters == null || !parameters.Any())
            throw new ArgumentNullException(nameof(parameters));

        if (actionUrl == null)
            throw new ArgumentNullException(nameof(actionUrl));

        if (parameters.ContainsKey("HTMLContent"))
        {
            return parameters["HTMLContent"].ToString();
        }

        string formId = "PaymentForm";
        StringBuilder formBuilder = new StringBuilder();
        formBuilder.Append($"<form id=\"{formId}\" name=\"{formId}\" action=\"{actionUrl}\" role=\"form\" method=\"POST\">");

        foreach (KeyValuePair<string, object> parameter in parameters)
        {
            if(parameter.Value != null)
                formBuilder.Append($"<input type=\"hidden\" name=\"{parameter.Key}\" value=\"{parameter.Value}\">");
        }

        formBuilder.Append("</form>");

        if (appendSubmitScript)
        {
            StringBuilder scriptBuilder = new StringBuilder();
            scriptBuilder.Append("<script>");
            scriptBuilder.Append($"document.{formId}.submit();");
            scriptBuilder.Append("</script>");
            formBuilder.Append(scriptBuilder.ToString());
        }

        return formBuilder.ToString();
    }
}
