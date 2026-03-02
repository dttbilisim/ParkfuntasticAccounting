using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ecommerce.Core.Helpers;

public class FileHelper
{
    private IConfiguration Configuration { get; }

    private IHostEnvironment Environment { get; }

    public FileHelper(IConfiguration configuration, IHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    [return: NotNullIfNotNull("path")]
    public string? GetFileUrl(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var uploadPath = GetUploadPath();

        path = path.Replace(uploadPath, "");

        if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri) || uri.IsAbsoluteUri)
        {
            return path;
        }

        var uriBuilder = new UriBuilder(new Uri(GetFileBaseUrl()));
        if (uriBuilder.Port is 80 or 443)
        {
            uriBuilder.Port = -1;
        }

        uriBuilder.Path = uriBuilder.Path.TrimEnd('/', '\\') + '/' + path.TrimStart('/', '\\');

        return uriBuilder.ToString();
    }

    public string GetFileBaseUrl()
    {
        return Configuration["FileUrl"]!;
    }

    [return: NotNullIfNotNull("path")]
    public string? GetEmailFileUrl(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri) || uri.IsAbsoluteUri)
        {
            return path;
        }

        var uriBuilder = new UriBuilder(new Uri(GetEmailFileBaseUrl()));
        if (uriBuilder.Port is 80 or 443)
        {
            uriBuilder.Port = -1;
        }

        uriBuilder.Path = uriBuilder.Path.TrimEnd('/', '\\') + '/' + path.TrimStart('/', '\\');

        return uriBuilder.ToString();
    }

    public string GetEmailFileBaseUrl()
    {
        var devUrl = Configuration["EmailSetting:DevUrl"];
        if (string.IsNullOrWhiteSpace(devUrl))
        {
            devUrl = "https://localhost";
        }

        var emailUrlBuilder = new UriBuilder(new Uri(devUrl));
        if (emailUrlBuilder.Port is 80 or 443)
        {
            emailUrlBuilder.Port = -1;
        }

        emailUrlBuilder.Path = null;
        emailUrlBuilder.Query = null;

        return emailUrlBuilder.ToString();
    }

    [return: NotNullIfNotNull("path")]
    public string? GetEmailFileAdminUrl(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (!Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out var uri) || uri.IsAbsoluteUri)
        {
            return path;
        }

        var uriBuilder = new UriBuilder(new Uri(GetEmailFileAdminBaseUrl()));
        if (uriBuilder.Port is 80 or 443)
        {
            uriBuilder.Port = -1;
        }

        uriBuilder.Path = uriBuilder.Path.TrimEnd('/', '\\') + '/' + path.TrimStart('/', '\\');

        return uriBuilder.ToString();
    }

    public string GetEmailFileAdminBaseUrl()
    {
        var adminUrl = Configuration["EmailSetting:AdminUrl"];
        if (string.IsNullOrWhiteSpace(adminUrl))
        {
            adminUrl = "https://localhost";
        }

        var emailUrlBuilder = new UriBuilder(new Uri(adminUrl));
        if (emailUrlBuilder.Port is 80 or 443)
        {
            emailUrlBuilder.Port = -1;
        }

        emailUrlBuilder.Path = null;
        emailUrlBuilder.Query = null;

        return emailUrlBuilder.ToString();
    }

    public string GetUploadPath()
    {
        var uploadPath = Configuration["UploadImagePath"];

        var solutionDirName = $"{Path.DirectorySeparatorChar}ecommerce";
        var solutionPath = Environment.ContentRootPath[..(Environment.ContentRootPath.IndexOf(solutionDirName, StringComparison.Ordinal) + solutionDirName.Length)];

        return Environment.IsDevelopment() && string.IsNullOrEmpty(uploadPath)
            ? Path.GetFullPath(Path.Combine(solutionPath, "Uploads"))
            : uploadPath!;
    }
}