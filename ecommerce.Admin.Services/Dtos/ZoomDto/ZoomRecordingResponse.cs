using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomRecordingResponse
{
    [JsonProperty("recording_files")]
    public List<ZoomRecordingFile> RecordingFiles { get; set; }
}

