using System.Text;
using Microsoft.AspNetCore.Components;

namespace ecommerce.Web.Services
{
    public interface ISeoService
    {
        void SetMetaTags(string title, string description, string keywords = null, string imageUrl = null);
        void SetCanonicalUrl(string url);
        void SetOpenGraphTags(string title, string description, string imageUrl = null);
    }

    public class SeoService : ISeoService
    {
        private readonly ILogger<SeoService> _logger;
        private readonly NavigationManager _navigationManager;

        public SeoService(ILogger<SeoService> logger, NavigationManager navigationManager)
        {
            _logger = logger;
            _navigationManager = navigationManager;
        }

        public void SetMetaTags(string title, string description, string keywords = null, string imageUrl = null)
        {
            var meta = new StringBuilder();
            
            // Title
            meta.AppendLine($"<title>{title}</title>");
            meta.AppendLine($"<meta name=\"title\" content=\"{title}\" />");
            
            // Description
            if (!string.IsNullOrEmpty(description))
            {
                meta.AppendLine($"<meta name=\"description\" content=\"{description}\" />");
            }
            
            // Keywords
            if (!string.IsNullOrEmpty(keywords))
            {
                meta.AppendLine($"<meta name=\"keywords\" content=\"{keywords}\" />");
            }
            
            // Image
            if (!string.IsNullOrEmpty(imageUrl))
            {
                meta.AppendLine($"<meta name=\"image\" content=\"{imageUrl}\" />");
            }

            UpdateMetaTags(meta.ToString());
        }

        public void SetCanonicalUrl(string url)
        {
            var absoluteUrl = new Uri(_navigationManager.BaseUri + url.TrimStart('/'));
            var canonical = $"<link rel=\"canonical\" href=\"{absoluteUrl}\" />";
            UpdateMetaTags(canonical);
        }

        public void SetOpenGraphTags(string title, string description, string imageUrl = null)
        {
            var meta = new StringBuilder();
            
            meta.AppendLine("<meta property=\"og:type\" content=\"website\" />");
            meta.AppendLine($"<meta property=\"og:title\" content=\"{title}\" />");
            meta.AppendLine($"<meta property=\"og:description\" content=\"{description}\" />");
            
            if (!string.IsNullOrEmpty(imageUrl))
            {
                meta.AppendLine($"<meta property=\"og:image\" content=\"{imageUrl}\" />");
            }

            // Twitter Cards
            meta.AppendLine("<meta name=\"twitter:card\" content=\"summary_large_image\" />");
            meta.AppendLine($"<meta name=\"twitter:title\" content=\"{title}\" />");
            meta.AppendLine($"<meta name=\"twitter:description\" content=\"{description}\" />");
            
            if (!string.IsNullOrEmpty(imageUrl))
            {
                meta.AppendLine($"<meta name=\"twitter:image\" content=\"{imageUrl}\" />");
            }

            UpdateMetaTags(meta.ToString());
        }

        private void UpdateMetaTags(string metaTags)
        {
            // JSRuntime üzerinden meta tag'leri güncelle
            try
            {
                // Not: Bu kısmı implement ederken JSRuntime injection ve ilgili JS interop methodları eklenecek
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Meta tags güncellenirken hata oluştu");
            }
        }
    }
}