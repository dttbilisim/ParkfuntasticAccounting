using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Concreate;
using Microsoft.Extensions.DependencyInjection;
namespace ecommerce.Virtual.Pos.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();

        return services;
    }
}
