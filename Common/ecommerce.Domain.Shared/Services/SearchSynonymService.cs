using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.Domain.Shared.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Domain.Shared.Models;
using Newtonsoft.Json;

namespace ecommerce.Domain.Shared.Services
{
    public class SearchSynonymService : ISearchSynonymService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private const string MetadataCacheKey = "SearchMetadata_Container";

        public SearchSynonymService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task<List<SearchSynonym>> GetAllSynonymsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            return (await unitOfWork.GetRepository<SearchSynonym>()
                .GetAllAsync(predicate: x => x.Status == 1)).ToList();
        }

        public async Task<Dictionary<string, List<string>>> GetSynonymDictionaryAsync()
        {
            var metadata = await GetSearchMetadataAsync();
            return metadata.Synonyms;
        }

        public async Task<SearchMetadataContainer> GetSearchMetadataAsync()
        {
            if (_cache.TryGetValue(MetadataCacheKey, out SearchMetadataContainer? container) && container != null)
            {
                return container;
            }

            // 1. Fetch all synonyms
            var synonyms = await GetAllSynonymsAsync();
            container = new SearchMetadataContainer();

            foreach (var item in synonyms)
            {
                var keyword = item.Keyword.Trim();
                var synonymList = item.Synonyms.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

                switch (item.Category)
                {
                    case SearchSynonymCategory.General:
                        if (!container.Synonyms.ContainsKey(keyword))
                            container.Synonyms[keyword] = new List<string>();

                        foreach (var s in synonymList)
                        {
                            if (!container.Synonyms[keyword].Contains(s, StringComparer.OrdinalIgnoreCase))
                                container.Synonyms[keyword].Add(s);

                            if (item.IsBidirectional)
                            {
                                if (!container.Synonyms.ContainsKey(s))
                                    container.Synonyms[s] = new List<string>();
                                
                                if (!container.Synonyms[s].Contains(keyword, StringComparer.OrdinalIgnoreCase))
                                    container.Synonyms[s].Add(keyword);
                            }
                        }
                        break;

                    case SearchSynonymCategory.RomanNumeral:
                        if (synonymList.Any() && !container.RomanNumerals.ContainsKey(keyword))
                        {
                            container.RomanNumerals[keyword] = synonymList.First();
                        }
                        break;

                    case SearchSynonymCategory.TechnicalVTerm:
                        foreach (var s in synonymList)
                        {
                            container.TechnicalVTerms.Add(s);
                        }
                        break;
                }
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();

                // 2. Fetch Boost Settings from AppSettings
                var boostSetting = await unitOfWork.GetRepository<AppSettings>()
                    .GetFirstOrDefaultAsync(predicate: x => x.Key == "Search_BoostWeights", disableTracking: true);
                
                if (boostSetting != null && !string.IsNullOrEmpty(boostSetting.Value))
                {
                    try
                    {
                        var serializedBoosts = JsonConvert.DeserializeObject<SearchBoostSettings>(boostSetting.Value);
                        if (serializedBoosts != null)
                        {
                            container.Boosts = serializedBoosts;
                        }
                    }
                    catch
                    {
                        // Fallback to default boosts if JSON is invalid
                    }
                }

                // 3. Fetch General Settings from AppSettings
                var generalSetting = await unitOfWork.GetRepository<AppSettings>()
                    .GetFirstOrDefaultAsync(predicate: x => x.Key == "Search_GeneralSettings", disableTracking: true);

                if (generalSetting != null && !string.IsNullOrEmpty(generalSetting.Value))
                {
                    try
                    {
                        var serializedGeneral = JsonConvert.DeserializeObject<SearchGeneralSettings>(generalSetting.Value);
                        if (serializedGeneral != null)
                        {
                            container.ShouldGroupOems = serializedGeneral.ShouldGroupOems;
                        }
                    }
                    catch
                    {
                        // Fallback to default
                    }
                }
            }

            // Fallbacks: If database is empty for these categories, populate with defaults
            if (!container.RomanNumerals.Any()) GetDefaultRomanNumerals(container.RomanNumerals);
            if (!container.TechnicalVTerms.Any()) GetDefaultTechnicalVTerms(container.TechnicalVTerms);

            _cache.Set(MetadataCacheKey, container, TimeSpan.FromMinutes(1));
            return container;
        }

        private void GetDefaultRomanNumerals(Dictionary<string, string> dict)
        {
            dict["I"] = "1"; dict["II"] = "2"; dict["III"] = "3"; dict["IV"] = "4";
            dict["V"] = "5"; dict["VI"] = "6"; dict["VII"] = "7"; dict["VIII"] = "8";
            dict["IX"] = "9"; dict["X"] = "10";
        }

        private void GetDefaultTechnicalVTerms(HashSet<string> set)
        {
            set.Add("kayiş"); set.Add("kayis"); set.Add("pk"); set.Add("belt"); set.Add("v-belt");
        }

        public async Task SaveGeneralSettingsAsync(SearchGeneralSettings settings)
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var settingRepo = unitOfWork.GetRepository<AppSettings>();
            
            var existingSetting = await settingRepo.GetFirstOrDefaultAsync(predicate: x => x.Key == "Search_GeneralSettings");

            var value = JsonConvert.SerializeObject(settings);

            if (existingSetting != null)
            {
                existingSetting.Value = value;
                settingRepo.Update(existingSetting);
            }
            else
            {
                await settingRepo.InsertAsync(new AppSettings
                {
                    Key = "Search_GeneralSettings",
                    Value = value,
                    Description = "Genel arama ayarları (Örn: OEM Gruplama)"
                });
            }

            await unitOfWork.SaveChangesAsync();
            await ClearCacheAsync();
        }

        public Task ClearCacheAsync()
        {
            _cache.Remove(MetadataCacheKey);
            return Task.CompletedTask;
        }
    }
}
