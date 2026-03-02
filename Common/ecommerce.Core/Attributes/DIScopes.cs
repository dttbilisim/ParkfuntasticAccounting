namespace ecommerce.Core.Attributes {
    [AttributeUsage(AttributeTargets.Class)]
    public class ScopedAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SingletonAttribute : Attribute {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class TransientAttribute : Attribute {

    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ServiceLogAttribute : Attribute {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SessionIdFlagAttribute : Attribute {

    }
}
