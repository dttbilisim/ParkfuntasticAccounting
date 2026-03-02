using ecommerce.Cargo.Mng.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using RestSharp;

namespace ecommerce.Cargo.Mng
{
    public partial class MngClient
    {
        private MngOptions Options { get; }

        private IMemoryCache Cache { get; }

        private MngRestClient MngRestClient { get; }

        public MngClient(IOptions<MngOptions> options, IMemoryCache cache)
        {
            Options = options.Value;
            Cache = cache;
            MngRestClient = new MngRestClient(Options.ClientId, Options.ClientSecret, Options.UseSandbox)
            {
                LoginDelegate = LoginAsync,
                LogoutDelegate = LogoutAsync
            };
        }

        private async Task LoginAsync(RestRequest request)
        {
            if (request.Resource == MngClientConstants.TokenPath)
            {
                return;
            }

            var cacheKey = string.Format(MngClientConstants.AuthCacheKey, Options.CustomerNumber);

            var cacheResult = await Cache.GetOrCreateAsync<MngSessionCacheItem>(
                cacheKey,
                async entry =>
                {
                    var tokenResponse = await MngRestClient.PostAsync<AuthResult>(
                        MngClientConstants.TokenPath,
                        new
                        {
                            customerNumber = Options.CustomerNumber,
                            password = Options.Password,
                            identityType = 1
                        },
                        authenticate: false
                    );

                    var cacheItem = new MngSessionCacheItem
                    {
                        AccessToken = tokenResponse.Data.Jwt,
                        RefreshToken = tokenResponse.Data.RefreshToken
                    };

                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromTicks((tokenResponse.Data.JwtExpireDate.AddHours(-3) - DateTime.UtcNow).Ticks);

                    return cacheItem;
                }
            );

            if (string.IsNullOrEmpty(cacheResult?.AccessToken))
            {
                return;
            }

            request.AddOrUpdateHeader(HeaderNames.Authorization, $"Bearer {cacheResult.AccessToken}");
        }

        private Task LogoutAsync()
        {
            Cache.Remove(string.Format(MngClientConstants.AuthCacheKey, Options.CustomerNumber));

            return Task.CompletedTask;
        }
    }
}