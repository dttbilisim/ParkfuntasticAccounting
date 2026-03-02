using System.Diagnostics.CodeAnalysis;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components;
namespace ecommerce.Core.Helpers;

public static class HtmlHelper
{
    [return: NotNullIfNotNull("content")]
    public static string? ModifyHtmlContentImages(FileHelper fileHelper, string? content, bool isWrite = false)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        var images = htmlDocument.DocumentNode.SelectNodes("//img");
        if (images != null)
        {
            var fileUrl = fileHelper.GetFileBaseUrl();

            foreach (var image in images)
            {
                var src = image.GetAttributeValue("src", null);
                if (src == null || (isWrite && !src.StartsWith(fileUrl))) continue;

                image.SetAttributeValue("src", isWrite ? src.Replace(fileUrl, "") : fileHelper.GetFileUrl(src));
            }
        }

        return htmlDocument.DocumentNode.OuterHtml;
    }

    [return: NotNullIfNotNull("content")]
    public static string? ModifyEmailContentImages(FileHelper fileHelper, string? content, bool isWrite = false)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        var images = htmlDocument.DocumentNode.SelectNodes("//img");
        if (images != null)
        {
            var fileUrl = fileHelper.GetEmailFileBaseUrl();

            foreach (var image in images)
            {
                var src = image.GetAttributeValue("src", null);
                if (src == null || (isWrite && !src.StartsWith(fileUrl))) continue;

                image.SetAttributeValue("src", isWrite ? src.Replace(fileUrl, "") : fileHelper.GetEmailFileUrl(src));
            }
        }

        return htmlDocument.DocumentNode.OuterHtml;
    }
    [return: NotNullIfNotNull("html")]
    public static MarkupString? MarkAsHtml(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return null;
        }

        return new MarkupString(html);
    }
}