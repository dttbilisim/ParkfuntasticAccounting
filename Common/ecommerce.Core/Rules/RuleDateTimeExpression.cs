using System.Globalization;
using System.Text.RegularExpressions;
using ecommerce.Core.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ecommerce.Core.Rules;

[JsonConverter(typeof(RuleDateTimeExpressionJsonConverter))]
public class RuleDateTimeExpression : IComparable<RuleDateTimeExpression>, IEquatable<RuleDateTimeExpression>
{
    private const double MillisecondsInADay = MillisecondsInAnHour * 24;
    private const double MillisecondsInAMinute = MillisecondsInASecond * 60;
    private const double MillisecondsInAMonthApproximate = MillisecondsInAYearApproximate / MonthsInAYear;
    private const double MillisecondsInAnHour = MillisecondsInAMinute * 60;
    private const double MillisecondsInASecond = 1000;
    private const double MillisecondsInAWeek = MillisecondsInADay * 7;
    private const double MillisecondsInAYearApproximate = MillisecondsInADay * 365;
    private const int MonthsInAYear = 12;

    public const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";

    public static readonly string[] DefaultDateTimeFormats =
    {
        "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK",
        "yyyy-MM-dd"
    };

    public static readonly Regex TimeExpressionRegex = new Regex(
        @"^(?<factor>[+\-]?(?:\d+))(?<interval>(?:y|M|w|d|h|m|s))$",
        RegexOptions.ExplicitCapture
    );

    private double _approximateSeconds;

    public DateTime? Date { get; private set; }

    public int? Factor { get; private set; }

    public RuleDateTimeExpressionUnit? Interval { get; private set; }

    public RuleDateTimeExpression(DateTime date)
    {
        Date = date;
    }

    public RuleDateTimeExpression(TimeSpan timeSpan, MidpointRounding rounding = MidpointRounding.AwayFromZero)
        : this(timeSpan.TotalMilliseconds, rounding)
    {
    }

    public RuleDateTimeExpression(double milliseconds, MidpointRounding rounding = MidpointRounding.AwayFromZero) =>
        SetWholeFactorIntervalAndSeconds(milliseconds, rounding);

    public RuleDateTimeExpression(int factor, RuleDateTimeExpressionUnit unit) =>
        SetWholeFactorIntervalAndSeconds(factor, unit, MidpointRounding.AwayFromZero);

    public RuleDateTimeExpression(string? expression, MidpointRounding rounding = MidpointRounding.AwayFromZero)
    {
        var match = TimeExpressionRegex.Match(expression ?? string.Empty);

        if (!match.Success)
        {
            if (DateTime.TryParseExact(expression, DefaultDateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                Date = dateTime;
                return;
            }

            throw new ArgumentException($"Expression '{expression}' string is invalid", nameof(expression));
        }

        var factor = match.Groups["factor"].Value;
        if (!double.TryParse(factor, NumberStyles.Any, CultureInfo.InvariantCulture, out var fraction))
            throw new ArgumentException($"Expression '{expression}' contains invalid factor: {factor}", nameof(expression));

        var intervalValue = match.Groups["interval"].Value;
        var interval = Enum.GetValues(typeof(RuleDateTimeExpressionUnit))
            .Cast<RuleDateTimeExpressionUnit>()
            .Single(u => u.GetStringValue() == intervalValue);

        SetWholeFactorIntervalAndSeconds(fraction, interval, rounding);
    }

    public static bool TryParse(string? expression, out RuleDateTimeExpression? dateTimeExpression)
    {
        try
        {
            dateTimeExpression = new RuleDateTimeExpression(expression);
            return true;
        }
        catch
        {
            dateTimeExpression = null;
            return false;
        }
    }

    public double ToSeconds()
    {
        return _approximateSeconds;
    }

    public TimeSpan ToTimeSpan()
    {
        return TimeSpan.FromSeconds(_approximateSeconds);
    }

    public int CompareTo(RuleDateTimeExpression? other)
    {
        if (Date.HasValue && other?.Date.HasValue == true)
        {
            return Date.Value.CompareTo(other.Date);
        }

        if (other == null) return 1;
        if (Math.Abs(_approximateSeconds - other._approximateSeconds) < double.Epsilon) return 0;
        if (_approximateSeconds < other._approximateSeconds) return -1;

        return 1;
    }

    public bool Equals(RuleDateTimeExpression? other)
    {
        if (Date.HasValue && other?.Date.HasValue == true)
        {
            return Date.Value.Equals(other.Date.Value);
        }

        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return Math.Abs(_approximateSeconds - other._approximateSeconds) < double.Epsilon;
    }

    public static implicit operator RuleDateTimeExpression(DateTime date) => new RuleDateTimeExpression(date);

    public static implicit operator RuleDateTimeExpression(TimeSpan span) => new RuleDateTimeExpression(span);

    public static implicit operator RuleDateTimeExpression(double milliseconds) => new RuleDateTimeExpression(milliseconds);

    public static implicit operator RuleDateTimeExpression?(string? expression) => expression == null ? null : new RuleDateTimeExpression(expression);

    public static implicit operator string?(RuleDateTimeExpression? expression) => expression?.ToString();

    private void SetWholeFactorIntervalAndSeconds(double factor, RuleDateTimeExpressionUnit interval, MidpointRounding rounding)
    {
        var fraction = factor;
        double milliseconds;

        // if the factor is already a whole number then use it
        if (TryGetIntegerGreaterThanZero(fraction, out var whole))
        {
            Factor = whole;
            Interval = interval;
            _approximateSeconds = interval switch
            {
                RuleDateTimeExpressionUnit.Second => whole,
                RuleDateTimeExpressionUnit.Minute => whole * (MillisecondsInAMinute / MillisecondsInASecond),
                RuleDateTimeExpressionUnit.Hour => whole * (MillisecondsInAnHour / MillisecondsInASecond),
                RuleDateTimeExpressionUnit.Day => whole * (MillisecondsInADay / MillisecondsInASecond),
                RuleDateTimeExpressionUnit.Week => whole * (MillisecondsInAWeek / MillisecondsInASecond),
                RuleDateTimeExpressionUnit.Month => whole * (MillisecondsInAMonthApproximate / MillisecondsInASecond),
                RuleDateTimeExpressionUnit.Year => whole * (MillisecondsInAYearApproximate / MillisecondsInASecond),
                _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, null)
            };

            return;
        }

        switch (interval)
        {
            case RuleDateTimeExpressionUnit.Second:
                milliseconds = factor * MillisecondsInASecond;
                break;
            case RuleDateTimeExpressionUnit.Minute:
                milliseconds = factor * MillisecondsInAMinute;
                break;
            case RuleDateTimeExpressionUnit.Hour:
                milliseconds = factor * MillisecondsInAnHour;
                break;
            case RuleDateTimeExpressionUnit.Day:
                milliseconds = factor * MillisecondsInADay;
                break;
            case RuleDateTimeExpressionUnit.Week:
                milliseconds = factor * MillisecondsInAWeek;
                break;
            case RuleDateTimeExpressionUnit.Month:
                if (TryGetIntegerGreaterThanZero(fraction, out whole))
                {
                    Factor = whole;
                    Interval = interval;
                    _approximateSeconds = whole * (MillisecondsInAMonthApproximate / MillisecondsInASecond);
                    return;
                }

                milliseconds = factor * MillisecondsInAMonthApproximate;
                break;
            case RuleDateTimeExpressionUnit.Year:
                if (TryGetIntegerGreaterThanZero(fraction, out whole))
                {
                    Factor = whole;
                    Interval = interval;
                    _approximateSeconds = whole * (MillisecondsInAYearApproximate / MillisecondsInASecond);
                    return;
                }

                fraction = fraction * MonthsInAYear;
                if (TryGetIntegerGreaterThanZero(fraction, out whole))
                {
                    Factor = whole;
                    Interval = RuleDateTimeExpressionUnit.Month;
                    _approximateSeconds = whole * (MillisecondsInAMonthApproximate / MillisecondsInASecond);
                    return;
                }

                milliseconds = factor * MillisecondsInAYearApproximate;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(interval), interval, null);
        }

        SetWholeFactorIntervalAndSeconds(milliseconds, rounding);
    }

    private void SetWholeFactorIntervalAndSeconds(double milliseconds, MidpointRounding rounding)
    {
        double fraction;
        int whole;

        if (milliseconds >= MillisecondsInAWeek)
        {
            fraction = milliseconds / MillisecondsInAWeek;
            if (TryGetIntegerGreaterThanZero(fraction, out whole))
            {
                Factor = whole;
                Interval = RuleDateTimeExpressionUnit.Week;
                _approximateSeconds = Factor.Value * (MillisecondsInAWeek / MillisecondsInASecond);
                return;
            }
        }

        if (milliseconds >= MillisecondsInADay)
        {
            fraction = milliseconds / MillisecondsInADay;
            if (TryGetIntegerGreaterThanZero(fraction, out whole))
            {
                Factor = whole;
                Interval = RuleDateTimeExpressionUnit.Day;
                _approximateSeconds = Factor.Value * (MillisecondsInADay / MillisecondsInASecond);
                return;
            }
        }

        if (milliseconds >= MillisecondsInAnHour)
        {
            fraction = milliseconds / MillisecondsInAnHour;
            if (TryGetIntegerGreaterThanZero(fraction, out whole))
            {
                Factor = whole;
                Interval = RuleDateTimeExpressionUnit.Hour;
                _approximateSeconds = Factor.Value * (MillisecondsInAnHour / MillisecondsInASecond);
                return;
            }
        }

        if (milliseconds >= MillisecondsInAMinute)
        {
            fraction = milliseconds / MillisecondsInAMinute;
            if (TryGetIntegerGreaterThanZero(fraction, out whole))
            {
                Factor = whole;
                Interval = RuleDateTimeExpressionUnit.Minute;
                _approximateSeconds = Factor.Value * (MillisecondsInAMinute / MillisecondsInASecond);
                return;
            }
        }

        if (milliseconds >= MillisecondsInASecond)
        {
            fraction = milliseconds / MillisecondsInASecond;
            if (TryGetIntegerGreaterThanZero(fraction, out whole))
            {
                Factor = whole;
                Interval = RuleDateTimeExpressionUnit.Second;
                _approximateSeconds = Factor.Value;
                return;
            }
        }

        // round to nearest second, using specified rounding
        Factor = Convert.ToInt32(Math.Round(milliseconds / MillisecondsInASecond, rounding));
        Interval = RuleDateTimeExpressionUnit.Second;
        _approximateSeconds = Factor.Value;
    }

    private static bool TryGetIntegerGreaterThanZero(double d, out int value)
    {
        if (Math.Abs(d % 1) < double.Epsilon)
        {
            value = Convert.ToInt32(d);
            return true;
        }

        value = 0;
        return false;
    }

    public static bool operator <(RuleDateTimeExpression left, RuleDateTimeExpression right) => left.CompareTo(right) < 0;

    public static bool operator <=(RuleDateTimeExpression left, RuleDateTimeExpression right) => left.CompareTo(right) < 0 || left.Equals(right);

    public static bool operator >(RuleDateTimeExpression left, RuleDateTimeExpression right) => left.CompareTo(right) > 0;

    public static bool operator >=(RuleDateTimeExpression left, RuleDateTimeExpression right) => left.CompareTo(right) > 0 || left.Equals(right);

    public static bool operator ==(RuleDateTimeExpression? left, RuleDateTimeExpression? right) => left?.Equals(right) ?? ReferenceEquals(right, null);

    public static bool operator !=(RuleDateTimeExpression left, RuleDateTimeExpression right) => !(left == right);

    public override string ToString()
    {
        if (Date.HasValue)
        {
            return Date.Value.ToString(DefaultDateTimeFormat, CultureInfo.InvariantCulture);
        }

        return Factor + Interval?.GetStringValue();
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((RuleDateTimeExpression) obj);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode()
    {
        return Date.HasValue ? Date.Value.GetHashCode() : _approximateSeconds.GetHashCode();
    }
}

public class RuleDateTimeExpressionJsonConverter : IsoDateTimeConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
        }
        else
        {
            writer.WriteValue(value.ToString());
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return reader.TokenType == JsonToken.Null ? null : new RuleDateTimeExpression(reader.Value!.ToString());
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(RuleDateTimeExpression);
    }
}