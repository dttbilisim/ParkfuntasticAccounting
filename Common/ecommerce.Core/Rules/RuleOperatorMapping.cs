using System.Collections;
using ecommerce.Core.Helpers;

namespace ecommerce.Core.Rules;

public static class RuleOperatorMapping
{
    private static readonly RuleOperatorMappingDictionary MappingDictionary = new();

    public static IEnumerable<RuleExpressionOperator> GetOperators(Type type) => MappingDictionary.PopulateAndGetOperators(type) ?? Array.Empty<RuleExpressionOperator>();

    public static IEnumerable<RuleExpressionOperator> GetOperators(RuleExpressionOperatorType type) => MappingDictionary.PopulateAndGetOperators(type) ?? Array.Empty<RuleExpressionOperator>();

    private sealed class RuleOperatorMappingDictionary
    {
        private readonly Dictionary<RuleExpressionOperatorType, RuleExpressionOperator[]> _typeMappings = new();

        private readonly Dictionary<Type, RuleExpressionOperator[]> _dataTypeMappings = new();

        private bool _isInitialized;

        private void EnsureMapping()
        {
            if (_isInitialized) return;

            lock (this)
            {
                if (_isInitialized)
                    return;
                PopulateMappings();
                _isInitialized = true;
            }
        }

        private void PopulateMappings()
        {
            _typeMappings.Add(
                RuleExpressionOperatorType.Common,
                new[]
                {
                    RuleExpressionOperator.Equal,
                    RuleExpressionOperator.NotEqual
                }
            );

            _typeMappings.Add(
                RuleExpressionOperatorType.Null,
                new[]
                {
                    RuleExpressionOperator.IsNull,
                    RuleExpressionOperator.IsNotNull
                }
            );

            _typeMappings.Add(
                RuleExpressionOperatorType.Array,
                new[]
                {
                    RuleExpressionOperator.In,
                    RuleExpressionOperator.NotIn
                }
            );

            _typeMappings.Add(
                RuleExpressionOperatorType.Numeric,
                new[]
                {
                    RuleExpressionOperator.GreaterThan,
                    RuleExpressionOperator.GreaterThanOrEqual,
                    RuleExpressionOperator.LessThan,
                    RuleExpressionOperator.LessThanOrEqual,
                }
            );

            _typeMappings.Add(
                RuleExpressionOperatorType.String,
                new[]
                {
                    RuleExpressionOperator.Contains,
                    RuleExpressionOperator.NotContains,
                    RuleExpressionOperator.StartsWith,
                    RuleExpressionOperator.EndsWith,
                }
            );


            _dataTypeMappings.Add(
                typeof(string),
                _typeMappings[RuleExpressionOperatorType.Common]
                    .Concat(_typeMappings[RuleExpressionOperatorType.String])
                    .Concat(_typeMappings[RuleExpressionOperatorType.Array])
                    .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                    .ToArray()
            );

            _dataTypeMappings.Add(
                typeof(bool),
                _typeMappings[RuleExpressionOperatorType.Common]
                    .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                    .ToArray()
            );

            var numericOperators = _typeMappings[RuleExpressionOperatorType.Common]
                .Concat(_typeMappings[RuleExpressionOperatorType.Numeric])
                .Concat(_typeMappings[RuleExpressionOperatorType.Array])
                .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                .ToArray();

            _dataTypeMappings.Add(typeof(byte), numericOperators);
            _dataTypeMappings.Add(typeof(short), numericOperators);
            _dataTypeMappings.Add(typeof(int), numericOperators);
            _dataTypeMappings.Add(typeof(long), numericOperators);
            _dataTypeMappings.Add(typeof(sbyte), numericOperators);
            _dataTypeMappings.Add(typeof(ushort), numericOperators);
            _dataTypeMappings.Add(typeof(uint), numericOperators);
            _dataTypeMappings.Add(typeof(ulong), numericOperators);
            _dataTypeMappings.Add(typeof(float), numericOperators);
            _dataTypeMappings.Add(typeof(double), numericOperators);
            _dataTypeMappings.Add(typeof(decimal), numericOperators);

            var dateTimeOperators = _typeMappings[RuleExpressionOperatorType.Common]
                .Concat(_typeMappings[RuleExpressionOperatorType.Numeric])
                .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                .ToArray();

            _dataTypeMappings.Add(typeof(DateTime), dateTimeOperators);
            _dataTypeMappings.Add(typeof(DateTimeOffset), dateTimeOperators);
            _dataTypeMappings.Add(typeof(TimeSpan), dateTimeOperators);
            _dataTypeMappings.Add(typeof(DateOnly), dateTimeOperators);
            _dataTypeMappings.Add(typeof(TimeOnly), dateTimeOperators);

            _dataTypeMappings.Add(
                typeof(Guid),
                _typeMappings[RuleExpressionOperatorType.Common]
                    .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                    .ToArray()
            );

            _dataTypeMappings.Add(
                typeof(Enum),
                _typeMappings[RuleExpressionOperatorType.Common]
                    .Concat(_typeMappings[RuleExpressionOperatorType.Array])
                    .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                    .ToArray()
            );

            _dataTypeMappings.Add(
                typeof(IEnumerable),
                _typeMappings[RuleExpressionOperatorType.Array]
                    .Concat(_typeMappings[RuleExpressionOperatorType.Null])
                    .ToArray()
            );
        }

        public RuleExpressionOperator[]? PopulateAndGetOperators(Type type)
        {
            EnsureMapping();

            var isNullable = TypeHelper.IsNullable(type);

            type = isNullable ? type.GenericTypeArguments[0] : type;

            if (type.IsEnum)
            {
                type = typeof(Enum);
            }
            else if (TypeHelper.IsEnumerable(type, out _, false))
            {
                type = typeof(IEnumerable);
            }

            var operators = _dataTypeMappings.GetValueOrDefault(type);

            if (!isNullable)
            {
                var nullableOperators = _typeMappings.GetValueOrDefault(RuleExpressionOperatorType.Null, Array.Empty<RuleExpressionOperator>());

                operators = operators?.Except(nullableOperators).ToArray();
            }

            return operators;
        }

        public RuleExpressionOperator[]? PopulateAndGetOperators(RuleExpressionOperatorType type)
        {
            EnsureMapping();

            return _typeMappings.GetValueOrDefault(type);
        }
    }
}