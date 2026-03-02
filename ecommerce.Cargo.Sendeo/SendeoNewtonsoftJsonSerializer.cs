using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RestSharp;
using RestSharp.Serializers;

namespace ecommerce.Cargo.Sendeo
{
    public class SendeoNewtonsoftJsonSerializer : IRestSerializer, ISerializer, IDeserializer
    {
        public static readonly DefaultContractResolver ContractResolver = new()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        public static readonly JsonSerializerSettings JsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ContractResolver = ContractResolver
        };

        public T? Deserialize<T>(RestResponse response)
        {
            return JsonConvert.DeserializeObject<T>(response.Content!, JsonSerializerSettings);
        }

        public string? Serialize(object? obj)
        {
            return obj == null ? null : JsonConvert.SerializeObject(obj, JsonSerializerSettings);
        }

        public string? Serialize(Parameter parameter) => Serialize(parameter.Value);

        public ContentType ContentType { get; set; } = ContentType.Json;

        public ISerializer Serializer => this;
        public IDeserializer Deserializer => this;
        public DataFormat DataFormat => DataFormat.Json;
        public string[] AcceptedContentTypes => ContentType.JsonAccept;
        public SupportsContentType SupportsContentType => contentType => contentType.Value.EndsWith("json", StringComparison.InvariantCultureIgnoreCase);
    }
}