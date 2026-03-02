using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ImageResult;
using ecommerce.Core.Utils.ResultSet;
using Humanizer;
using Microsoft.AspNetCore.Components.Forms;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using PointF = SixLabors.ImageSharp.PointF;
using Size = SixLabors.ImageSharp.Size;
using SolidBrush = SixLabors.ImageSharp.Drawing.Processing.SolidBrush;
using SixLabors.ImageSharp.PixelFormats;
using ImageMagick;

namespace ecommerce.Admin.Helpers.Interfaces
{
    public sealed class FileService : IFileService{
        public IAppSettingService AppSettingService{get;set;}
        public FileHelper FileHelper{get;}
        private int imageResizeWidth = 1000;
        private int imageResizeHeight = 1000;
        private int appSettingUploadFileSize;
        private long MaxFileSize = 1024 * 1024 * 5;
        private IConfiguration _configuration{get;set;}
        public FileService(IAppSettingService appSettingService, FileHelper fileHelper, IConfiguration configuration){
            AppSettingService = appSettingService;
            FileHelper = fileHelper;
            _configuration = configuration;
        }
        public async Task<IActionResult<ImageResponse>> UploadDiscountFile(IBrowserFile file){
            IActionResult<ImageResponse> response = new();
            var uploadResponse = await UploadFileWithResize(file, "DiscountImages");
            if(uploadResponse.Ok) response.Result = uploadResponse.Result;
            return response;
        }
        public async Task<IActionResult<ImageResponse>> UploadProductFile(IBrowserFile file){
            IActionResult<ImageResponse> response = new();
            var uploadResponse = await UploadFileWithResize(file, "ProductImages");
            if(uploadResponse.Ok) response.Result = uploadResponse.Result;
            return response;
        }
        public async Task<IActionResult<ImageResponse>> UploadStaticFile(IBrowserFile file){
            IActionResult<ImageResponse> response = new();
            var uploadResponse = await UploadFileWithResize(file, "StaticPageImages");
            if(uploadResponse.Ok) response.Result = uploadResponse.Result;
            return response;
        }
        public async Task CompressImage(Stream inputImageStream, string format, string path, bool IsWatermark,bool IsCompress){
            if(inputImageStream != null){
                using(var image = await Image.LoadAsync(inputImageStream)){
                    var watermarkText = "Yedeksen";
                    var opacity = 0.3f;
                    var watermarkColor = Color.FromRgba(255, 255, 255, (byte) (255 * opacity));
                    var watermarkBrush = new SolidBrush(watermarkColor);
                    var centerX = image.Width / 2;
                    var centerY = image.Height / 2;
                    var maxFontSize = Math.Min(image.Width, image.Height) * 0.2f;
                    var watermarkFont = SystemFonts.CreateFont("Arial", maxFontSize);

                    // Calculate the position to center the text
                    var textSize = TextMeasurer.MeasureBounds(watermarkText, new TextOptions(watermarkFont));
                    var xPosition = centerX - (textSize.Width / 2);
                    var yPosition = centerY - (textSize.Height / 2);
                    var sourceWidth = image.Width;
                    var sourceHeight = image.Height;
                    var rotatedTextImage = new Image<Rgba32>(image.Width, image.Height);
                    var rotationAngleDegrees = 0;

                    // Resmin oranı
                    var sourceAspectRatio = (float) sourceWidth / sourceHeight;
                    int cropX, cropY, cropWidth, cropHeight;
                    cropWidth = sourceWidth;
                    cropHeight = sourceWidth;
                    cropX = 0;
                    cropY = (sourceHeight - cropHeight) / 2;
                    var newWidth = image.Width;
                    var newHeight = image.Height;
                    double rate = 0;
                    if(newWidth > 1000){
                        rate = ((double) image.Width / 1000);
                    }
                    if(newHeight > 1000){
                        if(rate < (image.Height / 1000)){
                            rate = (image.Height / 1000);
                        }
                    }
                    if (rate > 0 && !format.Contains("gif"))
                    {
                        newHeight = (int)Math.Round((double)(image.Height / rate), 1);
                        newWidth = (int)Math.Round((double)(image.Width / rate), 1);
                        if (format.Contains("jpeg") || format.Contains("jpg"))
                        {
                            var jpegEncoder = new JpegEncoder
                            {
                                Quality = 50 // Sıkıştırma kalitesini belirleyin (0 ile 100 arasında)
                            };
                            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(newWidth, newHeight), Mode = ResizeMode.Max }));
                            // watermark ekleme icin
                            if (IsWatermark)
                            {
                                rotatedTextImage.Mutate(ctx => ctx.DrawText(new DrawingOptions() { GraphicsOptions = new GraphicsOptions { Antialias = true } }, watermarkText, watermarkFont, watermarkBrush, new PointF(xPosition, yPosition)).Rotate(rotationAngleDegrees));
                            }

                            // image.Mutate(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));
                            image.Mutate(ctx => ctx.DrawImage(rotatedTextImage, 1f));
                            await image.SaveAsync(path, jpegEncoder);
                            await ImageCompressMagick(path,IsCompress);
                        }
                        if (format.Contains("webp"))
                        {
                            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(newWidth, newHeight), Mode = ResizeMode.Max }));
                            // watermark ekleme icin
                            if (IsWatermark)
                            {
                                rotatedTextImage.Mutate(ctx => ctx.DrawText(new DrawingOptions() { GraphicsOptions = new GraphicsOptions { Antialias = true } }, watermarkText, watermarkFont, watermarkBrush, new PointF(xPosition, yPosition)).Rotate(rotationAngleDegrees));
                            }
                            //  image.Mutate(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));
                            image.Mutate(ctx => ctx.DrawImage(rotatedTextImage, 1f));
                            await image.SaveAsync(path);
                            await ImageCompressMagick(path,IsCompress);
                        }
                        if (format.Contains("png"))
                        {
                            var pngEncoder = new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression, };
                            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(newWidth, newHeight), Mode = ResizeMode.Max }));
                            // watermark ekleme icin
                            if (IsWatermark)
                            {
                                rotatedTextImage.Mutate(ctx => ctx.DrawText(new DrawingOptions() { GraphicsOptions = new GraphicsOptions { Antialias = true } }, watermarkText, watermarkFont, watermarkBrush, new PointF(xPosition, yPosition)).Rotate(rotationAngleDegrees));
                            }
                            //image.Mutate(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));
                            image.Mutate(ctx => ctx.DrawImage(rotatedTextImage, 1f));
                            await image.SaveAsync(path, pngEncoder);
                            await ImageCompressMagick(path,IsCompress);
                        }
                    }
                    else if (format.Contains("gif"))
                    {
                        await image.SaveAsGifAsync(path);
                    }
                    else
                    {
                        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(newWidth, newHeight), Mode = ResizeMode.Max }));
                        // watermark ekleme icin
                        if (IsWatermark)
                        {
                            rotatedTextImage.Mutate(ctx => ctx.DrawText(new DrawingOptions() { GraphicsOptions = new GraphicsOptions { Antialias = true } }, watermarkText, watermarkFont, watermarkBrush, new PointF(xPosition, yPosition)).Rotate(rotationAngleDegrees));
                        }
                        //  image.Mutate(ctx => ctx.Crop(new Rectangle(cropX, cropY, cropWidth, cropHeight)));
                        image.Mutate(ctx => ctx.DrawImage(rotatedTextImage, 1f));
                        await image.SaveAsync(path);
                        await ImageCompressMagick(path,IsCompress);
                    }
                }
            }
        }
        public async Task ImageCompressMagick(string imagePath,bool IsCompress){
            using var image = new MagickImage(imagePath);
            if(IsCompress){
                await image.WriteAsync(imagePath);
            } else{
                var compressionQuality = 80;
                image.Quality = (uint)compressionQuality;
                if(image.Height > 500 && image.Width > 500){
                    image.Resize(700, 700);
                }
            }
           
          
        }
        public async Task<IActionResult<ImageResponse>> UploadFileWithResize(IBrowserFile file, string uploadFolder){
            IActionResult<ImageResponse> response = new(){Result = new ImageResponse()};
            var resized = await file.RequestImageFileAsync(file.ContentType, imageResizeWidth, imageResizeHeight);
            if(resized.Size > MaxFileSize){
                response.AddError($"Dosya boyutu {MaxFileSize.Bytes().Humanize()} değerinden büyük olamaz.");
            }
            var uploadPath = FileHelper.GetUploadPath();
            var newFileName = await PrepareUniqueImageName(file);
            await DirectoryControl(Path.Combine(uploadPath, uploadFolder));
            var path = Path.Combine(uploadPath, uploadFolder, newFileName);
            var uploadResult = await UploadFile(file, path);
            return uploadResult;
        }
        public async Task<IActionResult<ImageResponse>> UploadFile(IBrowserFile file, string uploadFolder){
            var response = OperationResult.CreateResult<ImageResponse>();
            try{
                if(file.Size > MaxFileSize){
                    response.AddError($"Dosya boyutu {MaxFileSize.Bytes().Humanize()} değerinden büyük olamaz.");
                    return response;
                }
                var uploadPath = Path.Combine(FileHelper.GetUploadPath(), uploadFolder);
                var fileName = await PrepareUniqueImageName(file);
                await DirectoryControl(uploadPath);
                var filePath = Path.Combine(uploadPath, fileName);
                using(var stream = File.Create(filePath)){
                    await file.OpenReadStream(MaxFileSize).CopyToAsync(stream);
                }
                response.Result = new ImageResponse{FileName = file.Name, GuidFileName = fileName, Root = Path.Combine(uploadFolder, fileName)};
            } catch(Exception e){
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task GetProductAppSetting(){
            var imageUploadLimitResponse = await AppSettingService.GetValues("ProductImageUploadLimit", "ProductImageUploadResizeDimension");
            if(imageUploadLimitResponse.Ok){
                appSettingUploadFileSize = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ProductImageUploadLimit")?.Value);
                imageResizeWidth = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ProductImageUploadResizeDimension")?.Value.Split("x")[0]);
                imageResizeHeight = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ProductImageUploadResizeDimension")?.Value.Split("x")[1]);
                MaxFileSize = 1024 * 1024 * appSettingUploadFileSize;
            }
        }
        public Task<string> PrepareUniqueImageName(IBrowserFile file){
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(file.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return Task.FromResult(newFileName);
        }
        public Task DirectoryControl(string Root){
            var directoryPath = Path.Combine(Root);
            if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            return Task.CompletedTask;
        }
    }
}
