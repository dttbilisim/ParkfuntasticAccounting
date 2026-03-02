using ecommerce.Core.Utils.TypeList;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ecommerce.Core.Rules.Fields;

public class FieldDefinitionOptions
{
    public ITypeList<IFieldDefinitionProvider> Providers { get; } = new TypeList<IFieldDefinitionProvider>();

    public JsonSerializerSettings SerializerSettings { get; set; } = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        },
        MissingMemberHandling = MissingMemberHandling.Ignore,
        MaxDepth = 32,
        TypeNameHandling = TypeNameHandling.None,
    };
}