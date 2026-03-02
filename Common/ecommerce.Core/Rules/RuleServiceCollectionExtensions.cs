using System.Reflection;
using ecommerce.Core.Rules.Fields;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Core.Rules;

public static class RuleServiceCollectionExtensions
{
    public static IServiceCollection AddRules(
        this IServiceCollection services,
        Action<FieldDefinitionOptions>? configure = null,
        IEnumerable<string>? assemblyNames = null)
    {
        services.AddOptions<FieldDefinitionOptions>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddTransient<IRuleEngineRepository, RuleEngineRepository>();
        services.AddSingleton<IFieldDefinitionManager, FieldDefinitionManager>();

        if (assemblyNames != null)
        {
            foreach (var assemblyName in assemblyNames)
            {
                var assembly = Assembly.Load(assemblyName);

                var implementationTypes = assembly.GetTypes().Where(type =>
                    typeof(IFieldDefinitionProvider).IsAssignableFrom(type) ||
                    typeof(IFieldDefinitionValueProvider).IsAssignableFrom(type) ||
                    typeof(IFieldDefinitionValueOptionProvider).IsAssignableFrom(type));

                foreach (var implementationType in implementationTypes)
                {
                    services.AddTransient(implementationType);
                }
            }
        }

        return services;
    }
}