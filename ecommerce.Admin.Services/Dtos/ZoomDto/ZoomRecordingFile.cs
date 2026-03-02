using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomRecordingFile{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("download_url")]
    public string DownloadUrl { get; set; }

    [JsonProperty("recording_type")]
    public string RecordingType { get; set; }

    [JsonProperty("play_url")]
    public string PlayUrl { get; set; }

    [JsonProperty("file_size")]
    public long FileSize { get; set; }

    [JsonProperty("file_type")]
    public string FileType { get; set; }
}
