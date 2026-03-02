using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ecommerce.Admin.Helpers
{
    public static class ServiceCollectionExtensions
    {
        public static void AddProxiedScoped<TInterface, TImplementation>(this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.TryAddScoped<TImplementation>();
            services.TryAddScoped(typeof(TInterface), serviceProvider =>
            {
                var proxyGenerator = serviceProvider.GetRequiredService<IProxyGenerator>();
                var actual = serviceProvider.GetRequiredService<TImplementation>();
                var interceptors = serviceProvider.GetServices<IInterceptor>().ToArray();
                return proxyGenerator.CreateInterfaceProxyWithTarget(typeof(TInterface), actual, interceptors);
            });
        }
    }
}
