using System.Collections.Immutable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ecommerce.Core.Rules.Fields;

public class FieldDefinitionManager : IFieldDefinitionManager
{
    private Dictionary<string, FieldScopeDefinition> FieldScopeDefinitions => _lazyFieldScopeDefinitions.Value;
    private readonly Lazy<Dictionary<string, FieldScopeDefinition>> _lazyFieldScopeDefinitions;

    private FieldDefinitionOptions Options { get; }

    private readonly IServiceProvider _serviceProvider;

    public FieldDefinitionManager(
        IOptions<FieldDefinitionOptions> options,
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Options = options.Value;

        _lazyFieldScopeDefinitions = new Lazy<Dictionary<string, FieldScopeDefinition>>(
            CreateFieldScopeDefinitions,
            isThreadSafe: true
        );
    }

    public FieldDefinition GetField(string scope, string name)
    {
        var field = GetFieldOrNull(scope, name);

        if (field == null)
        {
            throw new ArgumentException("Undefined field: " + name);
        }

        return field;
    }

    public FieldDefinition? GetFieldOrNull(string scope, string name)
    {
        var paths = name.Split('.');
        FieldDefinition? field = null;

        var scopeDefinition = GetScope(scope);

        foreach (var path in paths)
        {
            field = field == null
                ? scopeDefinition.GetFieldOrNull(path)
                : field.GetOrNullChild(path);

            if (field == null)
                break;
        }

        return field;
    }

    public FieldScopeDefinition GetScope(string name)
    {
        var scope = GetScopeOrNull(name);

        if (scope == null)
        {
            throw new ArgumentException("Undefined scope: " + name);
        }

        return scope;
    }

    public FieldScopeDefinition? GetScopeOrNull(string name)
    {
        return FieldScopeDefinitions.GetValueOrDefault(name);
    }

    public IReadOnlyList<FieldDefinition> GetFields(string scope)
    {
        return GetScope(scope).Fields;
    }

    public IReadOnlyList<FieldScopeDefinition> GetScopes()
    {
        return FieldScopeDefinitions.Values.ToImmutableList();
    }

    private Dictionary<string, FieldScopeDefinition> CreateFieldScopeDefinitions()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = new FieldDefinitionContext(scope.ServiceProvider);

            var providers = Options
                .Providers
                .Select(p => (IFieldDefinitionProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, p))
                .ToList();

            foreach (var provider in providers)
            {
                provider.Define(context);
            }

            foreach (var provider in providers)
            {
                provider.AfterDefine(context);
            }

            return context.Scopes;
        }
    }
}