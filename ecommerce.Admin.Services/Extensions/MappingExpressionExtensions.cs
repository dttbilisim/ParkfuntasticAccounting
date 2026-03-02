using AutoMapper;

namespace ecommerce.Admin.Domain.Extensions;

public static class MappingExpressionExtensions
{
    public static IMappingExpression<TSource, TDestination> ForAllMembersIncluding<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression, Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions)
    {
        expression.ForAllMembers(memberOptions);

        return expression;
    }
}