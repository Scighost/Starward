using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

public abstract class TravelersDiaryBase
{

    [JsonPropertyName("uid")]
    public long Uid { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }

    /// <summary>
    /// 米游社 ID
    /// </summary>
    [JsonPropertyName("account_id")]
    public long AccountId { get; set; }


    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }

    /// <summary>
    /// 当前日期
    /// </summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(TravelersDiaryDateJsonConverter))]
    public DateTime Date { get; set; }

    /// <summary>
    /// 当前月
    /// </summary>
    [JsonPropertyName("month")]
    public int CurrentMonth { get; set; }

    /// <summary>
    /// 可查询月份
    /// </summary>
    [JsonPropertyName("optional_month")]
    public List<int> OptionalMonth { get; set; }

    /// <summary>
    /// 获取的数据所在的月份
    /// </summary>
    [JsonPropertyName("data_month")]
    public int DataMonth { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}


internal class TravelersDiaryDateJsonConverter : JsonConverter<DateTime>
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
