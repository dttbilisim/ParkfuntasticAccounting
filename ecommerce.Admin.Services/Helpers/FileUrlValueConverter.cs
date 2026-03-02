using AutoMapper;
using ecommerce.Core.Helpers;

namespace ecommerce.Admin.Domain.Helpers;

public class FileUrlValueConverter : IValueConverter<string, string>
{
    private readonly FileHelper _fileHelper;

    public FileUrlValueConverter(FileHelper fileHelper)
    {
        _fileHelper = fileHelper;
    }

    public string Convert(string sourceMember, ResolutionContext context)
    {
        return string.IsNullOrEmpty(sourceMember) ? sourceMember : _fileHelper.GetFileUrl(sourceMember);
    }
}