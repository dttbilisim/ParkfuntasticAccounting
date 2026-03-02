namespace ecommerce.Core.Rules.Fields;

public interface IFieldDefinitionProvider
{
    void Define(IFieldDefinitionContext context);

    void AfterDefine(IFieldDefinitionContext context);
}