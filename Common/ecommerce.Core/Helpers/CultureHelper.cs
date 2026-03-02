using System.Globalization;
using ecommerce.Core.Utils.Threading;

namespace ecommerce.Core.Helpers;

public static class CultureHelper
{
    public static IDisposable Use(string culture, string? uiCulture = null)
    {
        return Use(
            new CultureInfo(culture),
            uiCulture == null
                ? null
                : new CultureInfo(uiCulture)
        );
    }

    public static IDisposable Use(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        var currentCulture = CultureInfo.CurrentCulture;
        var currentUiCulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture ?? culture;

        return new DisposeAction(
            () =>
            {
                CultureInfo.CurrentCulture = currentCulture;
                CultureInfo.CurrentUICulture = currentUiCulture;
            }
        );
    }

    public static bool IsRtl => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

    public static bool IsValidCultureCode(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return false;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(cultureCode);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    public static string GetBaseCultureName(string cultureName)
    {
        return cultureName.Split('-')[0];
    }
}