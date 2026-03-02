namespace ecommerce.Core.Rules.Fields;

public class FieldDefinitionValueSelectList
{
    public bool Multiple { get; }

    public Type? OptionProviderType { get; }

    public IEnumerable<FieldDefinitionValueSelectListOption> Options { get; }

    public FieldDefinitionValueSelectList(IEnumerable<FieldDefinitionValueSelectListOption> options, bool multiple = false)
    {
        Options = options;
        Multiple = multiple;
    }

    public FieldDefinitionValueSelectList(Type optionProviderType, bool multiple = false)
    {
        OptionProviderType = optionProviderType.IsAssignableTo(typeof(IFieldDefinitionValueOptionProvider)) ? optionProviderType : throw new Exception("OptionProviderType must be assignable to IFieldDefinitionValueOptionProvider");
        Multiple = multiple;
        Options = new List<FieldDefinitionValueSelectListOption>();
    }
}

public class FieldDefinitionValueSelectListOption
{
    public string Text { get; set; }
    public string Value { get; set; }

    public FieldDefinitionValueSelectListOption(string text, string value)
    {
        Text = text;
        Value = value;
    }
}

public class FieldDefinitionValueSelectPagedList
{
    public IEnumerable<FieldDefinitionValueSelectListOption> Data { get; set; }

    public int Count { get; set; }

    public FieldDefinitionValueSelectPagedList()
    {
        Data = new List<FieldDefinitionValueSelectListOption>();
    }
}