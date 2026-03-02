namespace ImageDownload.Abstract;
public interface IImageStorageService{
    Task<string?> SaveImageAsync(string imageUrl, string fileName);
}
