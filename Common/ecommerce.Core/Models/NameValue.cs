namespace ecommerce.Core.Models
{
    [Serializable]
    public class NameValue
    {
        public string Name { get; set; } = null!;

        public string Value { get; set; } = null!;

        public NameValue()
        {
        }

        public NameValue(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    [Serializable]
    public class NameValue<T> : NameValue<T, string>
    {
        public NameValue()
        {
        }

        public NameValue(T name, string value)
            : base(name, value)
        {
        }
    }

    [Serializable]
    public class NameValue<T1, T2>
    {
        public T1 Name { get; set; } = default!;

        public T2 Value { get; set; } = default!;

        public NameValue()
        {
        }

        public NameValue(T1 name, T2 value)
        {
            Name = name;
            Value = value;
        }
    }
}