namespace ImageDownload.Abstract;
public interface IGoogleImageFetcher{
    Task<string?> GetFirstImageUrlAsync(string query);
}
