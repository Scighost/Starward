using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

/// <summary>
/// 旅行记录原石或摩拉获取记录
/// </summary>
public class TravelersDiaryAwardItem
{

    [JsonIgnore]
    public int Id { get; set; }

    public long Uid { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    /// <summary>
    /// 1原石，2摩拉
    /// </summary>
    public int Type { get; set; }


    [JsonPropertyName("action_id")]
    public int ActionId { get; set; }


    [JsonPropertyName("action")]
    public string ActionName { get; set; }

    /// <summary>
    /// 获取时间，UTC+8
    /// </summary>
    [JsonPropertyName("time"), JsonConverter(typeof(TravelersDiaryDateTimeJsonConverter))]
    public DateTime Time { get; set; }


    [JsonPropertyName("num")]
    public int Number { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}


internal class TravelersDiaryDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}
