using ecommerce.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ecommerce.Core.Extensions {
    public static class ConfigurationExtensions
    {

        public static IServiceCollection AddAppServices(this IServiceCollection services, string dllFileName)
        {
            var types = Assembly.Load(dllFileName).GetTypes()
                .Where(x => x.CustomAttributes != null &&
                            x.CustomAttributes.Any(y => y.AttributeType.Namespace != null &&
                                                        y.AttributeType.Namespace.Equals(typeof(ScopedAttribute).Namespace))).ToList();

            Add(services, types);
            return services;
        }

        private static void Add(IServiceCollection services, List<Type> types)
        {
            foreach (var implementType in types)
            {
                var interfaceType = implementType.GetInterface("I" + implementType.Name);
                if (interfaceType == null) continue;

                var toBeAdded = implementType.CustomAttributes.FirstOrDefault(c => c.AttributeType == typeof(ScopedAttribute) ||
                                                                                   c.AttributeType == typeof(TransientAttribute) ||
                                                                                   c.AttributeType == typeof(SingletonAttribute));
                if (toBeAdded == null) continue;

                var attribute = toBeAdded.AttributeType.Name.Replace("Attribute", "");
                //AddProxiedScoped(services, interfaceType, implementType, attribute.ToServiceLifetime());
                services.Add(new ServiceDescriptor(interfaceType, implementType,
                    attribute.ToServiceLifetime()));
            }
        }

        public static ServiceLifetime ToServiceLifetime(this string attribute)
        {
            switch (attribute)
            {
                case "Transient":
                    return ServiceLifetime.Transient;
                case "Scoped":
                    return ServiceLifetime.Scoped;
                case "Singleton":
                    return ServiceLifetime.Singleton;
                default:
                    return ServiceLifetime.Transient;
            }
        }


    }
}
