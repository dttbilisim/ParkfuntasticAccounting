using ecommerce.Core.Utils.ImageResult;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components.Forms;
namespace ecommerce.Admin.Helpers.Concretes
{
    public interface IFileService
    {
        Task<IActionResult<ImageResponse>> UploadDiscountFile(IBrowserFile file);

        Task<IActionResult<ImageResponse>> UploadProductFile(IBrowserFile file);

        Task<IActionResult<ImageResponse>> UploadStaticFile(IBrowserFile file);

        Task<IActionResult<ImageResponse>> UploadFileWithResize(IBrowserFile file, string uploadFolder);

        Task<IActionResult<ImageResponse>> UploadFile(IBrowserFile file, string uploadFolder);

        Task<string> PrepareUniqueImageName(IBrowserFile file);

        Task DirectoryControl(string Root);
        Task CompressImage(Stream inputImageStream, string format, string path,bool IsWatermark,bool IsCompress);
        
        
    }
}