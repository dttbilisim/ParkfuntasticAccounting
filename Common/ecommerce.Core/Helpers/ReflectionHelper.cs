using System.Reflection;

namespace ecommerce.Core.Helpers;

public static class ReflectionHelper
{
    /// <summary>
    /// Checks whether <paramref name="givenType"/> implements/inherits <paramref name="genericType"/>.
    /// </summary>
    /// <param name="givenType">Type to check</param>
    /// <param name="genericType">Generic type</param>
    public static bool IsAssignableToGenericType(Type givenType, Type genericType)
    {
        var givenTypeInfo = givenType.GetTypeInfo();

        if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
        {
            return true;
        }

        foreach (var interfaceType in givenTypeInfo.GetInterfaces())
        {
            if (interfaceType.GetTypeInfo().IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
            {
                return true;
            }
        }

        if (givenTypeInfo.BaseType == null)
        {
            return false;
        }

        return IsAssignableToGenericType(givenTypeInfo.BaseType, genericType);
    }

    public static List<Type> GetImplementedGenericTypes(Type givenType, Type genericType)
    {
        var result = new List<Type>();
        AddImplementedGenericTypes(result, givenType, genericType);
        return result;
    }

    private static void AddImplementedGenericTypes(List<Type> result, Type givenType, Type genericType)
    {
        var givenTypeInfo = givenType.GetTypeInfo();

        if (givenTypeInfo.IsGenericType && givenType.GetGenericTypeDefinition() == genericType && !result.Contains(givenType))
        {
            result.Add(givenType);
        }

        foreach (var interfaceType in givenTypeInfo.GetInterfaces())
        {
            if (interfaceType.GetTypeInfo().IsGenericType
                && interfaceType.GetGenericTypeDefinition() == genericType
                && !result.Contains(interfaceType))
            {
                result.Add(interfaceType);
            }
        }

        if (givenTypeInfo.BaseType == null)
        {
            return;
        }

        AddImplementedGenericTypes(result, givenTypeInfo.BaseType, genericType);
    }

    /// <summary>
    /// Tries to gets an of attribute defined for a class member and it's declaring type including inherited attributes.
    /// Returns default value if it's not declared at all.
    /// </summary>
    /// <typeparam name="TAttribute">Type of the attribute</typeparam>
    /// <param name="memberInfo">MemberInfo</param>
    /// <param name="defaultValue">Default value (null as default)</param>
    /// <param name="inherit">Inherit attribute from base classes</param>
    public static TAttribute? GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute? defaultValue = default, bool inherit = true)
        where TAttribute : Attribute
    {
        //Get attribute on the member
        if (memberInfo.IsDefined(typeof(TAttribute), inherit))
        {
            return memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).Cast<TAttribute>().First();
        }

        return defaultValue;
    }

    /// <summary>
    /// Tries to gets an of attribute defined for a class member and it's declaring type including inherited attributes.
    /// Returns default value if it's not declared at all.
    /// </summary>
    /// <typeparam name="TAttribute">Type of the attribute</typeparam>
    /// <param name="memberInfo">MemberInfo</param>
    /// <param name="defaultValue">Default value (null as default)</param>
    /// <param name="inherit">Inherit attribute from base classes</param>
    public static TAttribute? GetSingleAttributeOfMemberOrDeclaringTypeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute? defaultValue = default, bool inherit = true)
        where TAttribute : class
    {
        return memberInfo.GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
               ?? memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>().FirstOrDefault()
               ?? defaultValue;
    }

    /// <summary>
    /// Tries to gets attributes defined for a class member and it's declaring type including inherited attributes.
    /// </summary>
    /// <typeparam name="TAttribute">Type of the attribute</typeparam>
    /// <param name="memberInfo">MemberInfo</param>
    /// <param name="inherit">Inherit attribute from base classes</param>
    public static IEnumerable<TAttribute> GetAttributesOfMemberOrDeclaringType<TAttribute>(MemberInfo memberInfo, bool inherit = true)
        where TAttribute : class
    {
        var customAttributes = memberInfo.GetCustomAttributes(true).OfType<TAttribute>();
        var declaringTypeCustomAttributes =
            memberInfo.DeclaringType?.GetTypeInfo().GetCustomAttributes(true).OfType<TAttribute>();
        return declaringTypeCustomAttributes != null
            ? customAttributes.Concat(declaringTypeCustomAttributes).Distinct()
            : customAttributes;
    }
}