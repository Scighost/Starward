using System.Text.Json.Serialization;

namespace Starward.Core.DeviceFp;

public class DeviceExtListResult
{

    [JsonPropertyName("code")]
    public int Code { get; set; }


    [JsonPropertyName("msg")]
    public string Message { get; set; }


    [JsonPropertyName("ext_list")]
    public List<string> ExtList { get; set; }

}