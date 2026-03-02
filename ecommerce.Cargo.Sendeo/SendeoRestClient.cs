using System.Net;
using ecommerce.Cargo.Sendeo.Exceptions;
using ecommerce.Cargo.Sendeo.Models;
using ecommerce.Core.Extensions;
using RestSharp;

namespace ecommerce.Cargo.Sendeo
{
    public sealed class SendeoRestClient : RestClient
    {
        public Func<RestRequest, Task>? LoginDelegate { get; init; }

        public Func<Task>? LogoutDelegate { get; init; }

        private static readonly Dictionary<string, string> RequestHeaders = new()
        {
            { "Accept", "application/json" }
        };

        public SendeoRestClient() : base(
            new RestClientOptions(SendeoClientConstants.BaseUrl)
            {
                CookieContainer = new CookieContainer(),
                UserAgent = SendeoClientConstants.UserAgent,
                MaxTimeout = (int)TimeSpan.FromMinutes(2).TotalMilliseconds
            },
            configureSerialization: s => s.UseSerializer<SendeoNewtonsoftJsonSerializer>())
        {
            this.AddDefaultHeaders(RequestHeaders);
        }

        public async Task<RestResponse<T>> GetAsync<T>(
            string requestUri,
            object? options = null,
            IDictionary<string, string>? urlSegments = null,
            IDictionary<string, string>? headers = null,
            bool authenticate = true)
        {
            var request = new RestRequest(requestUri, Method.Get);

            await SetParams(request, urlSegments: urlSegments, options: options, headers: headers, authenticate: authenticate);

            return await SendAsync<T>(request);
        }

        public async Task<RestResponse<T>> DeleteAsync<T>(
            string requestUri,
            IDictionary<string, string>? urlSegments = null,
            object? options = null,
            IDictionary<string, string>? headers = null,
            bool authenticate = true)
        {
            var request = new RestRequest(requestUri, Method.Delete);

            await SetParams(request, urlSegments: urlSegments, options: options, headers: headers, authenticate: authenticate);

            return await SendAsync<T>(request);
        }

        public async Task<RestResponse<T>> PostAsync<T>(
            string requestUri,
            object value,
            IDictionary<string, string>? urlSegments = null,
            bool isJson = true,
            object? options = null,
            IDictionary<string, string>? headers = null,
            bool authenticate = true)
        {
            var request = new RestRequest(requestUri, Method.Post);

            await SetParams(request, value, urlSegments, isJson, options, headers, authenticate);

            return await SendAsync<T>(request);
        }

        public async Task<RestResponse<T>> PutAsync<T>(
            string requestUri,
            object value,
            IDictionary<string, string>? urlSegments = null,
            bool isJson = true,
            object? options = null,
            IDictionary<string, string>? headers = null,
            bool authenticate = true)
        {
            var request = new RestRequest(requestUri, Method.Put);

            await SetParams(request, value, urlSegments, isJson, options, headers, authenticate);

            return await SendAsync<T>(request);
        }

        public async Task<RestResponse<T>> PatchAsync<T>(
            string requestUri,
            object value,
            IDictionary<string, string>? urlSegments = null,
            bool isJson = true,
            object? options = null,
            IDictionary<string, string>? headers = null,
            bool authenticate = true)
        {
            var request = new RestRequest(requestUri, Method.Patch);

            await SetParams(request, value, urlSegments, isJson, options, headers, authenticate);

            return await SendAsync<T>(request);
        }

        private async Task SetParams(
            RestRequest request,
            object? value = null,
            IDictionary<string, string>? urlSegments = null,
            bool isJson = true,
            object? options = null,
            IDictionary<string, string>? headers = null,
            bool authenticate = true)
        {
            AddBody(request, value, isJson);
            AddUrlSegments(request, urlSegments);
            AddQueryParameters(request, options);
            AddHeaders(request, headers);

            if (authenticate)
            {
                await LoginAsync(request);
            }
        }

        private static void AddBody(RestRequest request, object? value, bool isJson = true)
        {
            if (value == null) return;

            if (isJson)
            {
                request.AddJsonBody(value);
            }
            else
            {
                request.AlwaysMultipartFormData = true;

                foreach (var pair in value.ToKeyValueString())
                {
                    if (pair.Value == null) continue;

                    request.AddParameter(pair.Key, pair.Value);
                }
            }
        }

        private static void AddUrlSegments(RestRequest request, IDictionary<string, string>? urlSegments)
        {
            if (urlSegments == null) return;

            foreach (var pair in urlSegments)
            {
                request.AddUrlSegment(pair.Key, pair.Value, false);
            }
        }

        private static void AddQueryParameters(RestRequest request, object? options)
        {
            if (options == null) return;

            foreach (var pair in options.ToKeyValueString())
            {
                if (pair.Value != null)
                {
                    request.AddQueryParameter(pair.Key, pair.Value);
                }
            }
        }

        private static void AddHeaders(RestRequest request, IDictionary<string, string>? headers)
        {
            if (headers == null) return;

            request.AddHeaders(headers);
        }

        private async Task<RestResponse<T>> SendAsync<T>(RestRequest request)
        {
            var response = await this.ExecuteAsync<T>(request);

            if (!response.IsSuccessful)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await LogoutAsync();
                }

                throw CreateException(request, response);
            }

            return response;
        }

        private SendeoApiException CreateException(RestRequest request, RestResponse response)
        {
            var errorMessage = response.ErrorMessage;

            try
            {
                var errorResponse = this.Deserialize<ResponseBase>(response).Data;

                errorMessage = errorResponse?.ExceptionMessage ?? errorMessage;
            }
            catch
            {
                // ignored
            }

            return new SendeoApiException(errorMessage, response.ErrorException)
            {
                HttpStatusCode = (int) response.StatusCode,
                RequestUrl = response.ResponseUri?.ToString() ?? request.Resource,
                ApiErrorMsg = response.ErrorMessage
            };
        }

        private async Task LoginAsync(RestRequest request)
        {
            if (LoginDelegate != null)
            {
                await LoginDelegate(request);
            }
        }

        private async Task LogoutAsync()
        {
            if (LogoutDelegate != null)
            {
                await LogoutDelegate();
            }
        }
    }
}