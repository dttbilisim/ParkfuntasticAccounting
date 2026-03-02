using System.Drawing;
using System.Drawing.Imaging;
using ecommerce.Admin.Domain.Dtos.ProductImageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Radzen;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using Rectangle = SixLabors.ImageSharp.Rectangle;
using SystemFonts = SixLabors.Fonts.SystemFonts;
namespace ecommerce.Admin.Controllers;
public class ImageController : Controller{
    private readonly HttpClient _httpClient;
    private readonly ImageProcessingService _imageProcessingService;
    private readonly IFileService _fileService;
    private readonly IConfiguration _configuration;
    private readonly IProductImageService _productImageService;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    protected ProductImageUpsertDto productImage = new();
    public ImageController(HttpClient httpClient, ImageProcessingService imageProcessingService, IFileService fileService, IConfiguration configuration, IProductImageService productImageService, IUnitOfWork<ApplicationDbContext> context){
        _httpClient = httpClient;
        _imageProcessingService = imageProcessingService;
        _fileService = fileService;
        _configuration = configuration;
        _productImageService = productImageService;
        _context = context;
    }
    [HttpPost("ImageDownload")]
    public async Task<string> ProcessImage([FromBody] ProductImageUrlDto input){
        var path = "";
        try{

            if(string.IsNullOrWhiteSpace(input.Barcode) && string.IsNullOrWhiteSpace(input.ImageUrl)){
                var productData = await _context.DbContext.Product.FirstOrDefaultAsync(x => x.Barcode.Trim().Contains(input.Barcode.Trim()));
                if(productData != null){
                    var response = await _httpClient.GetAsync(input.ImageUrl);
                   // response.EnsureSuccessStatusCode();
                    var imageBytes = await response.Content.ReadAsByteArrayAsync();
                    var inputStream = new MemoryStream(imageBytes);
                    await DirectoryControl();
                    var contentType = response.Content.Headers.ContentType.MediaType;
               
                    switch(contentType){
                        case "image/jpeg":
                        case "image/jpg":
                            path = Path.Combine(_configuration.GetValue<string>("UploadImagePath"), "ProductImages", input.Barcode+".jpg");
                            await _fileService.CompressImage(inputStream, "jpg", path, true, true);
                            break;
                        case "image/png":
                            path = Path.Combine(_configuration.GetValue<string>("UploadImagePath"), "ProductImages", input.Barcode+".png");
                            await _fileService.CompressImage(inputStream, "png", path, true, true);
                            break;
                        case "image/webp":
                            path = Path.Combine(_configuration.GetValue<string>("UploadImagePath"), "ProductImages", input.Barcode+".webp");
                            await _fileService.CompressImage(inputStream, "webp", path, true, true);
                            break;
                    }
                
               
                } else{
                    return "notimage";
                }
            }
           
        } catch(Exception e){
            Console.WriteLine(e.Message);
            throw;
        }
        
        return path.Replace("/","\\");
    }
    private async Task DirectoryControl(){
        var directoryPath = Path.Combine(_configuration.GetValue<string>("UploadImagePath"), "ProductImages");
        if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
    }
    private async Task<string> PrepareUniqueImageName(string name){
        var randomName = Path.GetRandomFileName();
        var extension = Path.GetExtension(name);
        var newFileName = Path.ChangeExtension(randomName, extension);
        return newFileName;
    }
}
