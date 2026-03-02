using System.Diagnostics.CodeAnalysis;
using Slugify;

namespace ecommerce.Core.Extensions;

public static class StringExtensions
{
    private static readonly SlugHelper SlugHelper = new(
        new SlugHelperConfiguration
        {
            StringReplacements = new Dictionary<string, string>
            {
                { " ", "-" },
                { "ı", "i" },
            }
        }
    );

    [return: NotNullIfNotNull("title")]
    public static string? ToFriendlyTitle(this string? title)
    {
        return title == null ? title : SlugHelper.GenerateSlug(title);
    }
}