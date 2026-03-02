using ecommerce.Core.Utils;
namespace ecommerce.Virtual.Pos.Abstract;
public interface IPaymentProviderFactory{
    IPaymentProvider Create(BankNames bankName);
    string CreatePaymentFormHtml(IDictionary<string, object> parameters, Uri actionUrl, bool appendFormSubmitScript = true);
}
