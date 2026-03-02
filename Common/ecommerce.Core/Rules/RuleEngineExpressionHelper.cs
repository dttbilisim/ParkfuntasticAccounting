namespace ecommerce.Core.Rules;

public static class RuleEngineExpressionHelper
{
    public static bool StartsWith(string value, string compareValue)
    {
        return value.StartsWith(compareValue);
    }

    public static bool EndsWith(string value, string compareValue)
    {
        return value.EndsWith(compareValue);
    }

    public static bool Contains(string value, string compareValue)
    {
        return value.Contains(compareValue);
    }

    public static bool NotContains(string value, string compareValue)
    {
        return !value.Contains(compareValue);
    }

    public static bool In<T>(T value, IEnumerable<object> compareValue)
    {
        return compareValue.Contains(value!);
    }

    public static bool NotIn<T>(T value, IEnumerable<object> compareValue)
    {
        return !compareValue.Contains(value!);
    }

    public static bool ArrayIn<T>(IEnumerable<T>? value, IEnumerable<object>? compareValue)
    {
        if (value == null || compareValue == null)
        {
            return false;
        }

        return compareValue.All(value.Cast<object>().Contains);
    }

    public static bool ArrayNotIn<T>(IEnumerable<T>? value, IEnumerable<object>? compareValue)
    {
        if (value == null || compareValue == null)
        {
            return false;
        }

        return !compareValue.All(value.Cast<object>().Contains);
    }
}