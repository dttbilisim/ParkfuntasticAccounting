namespace ecommerce.Core.Rules.Fields;

public abstract class FieldDefinitionProvider : IFieldDefinitionProvider
{
    public abstract void Define(IFieldDefinitionContext context);

    public virtual void AfterDefine(IFieldDefinitionContext context)
    {
    }
}