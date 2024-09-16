using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterInfo
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }


    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 难度
    /// </summary>
    [JsonPropertyName("difficulty_id")]
    public int DifficultyId { get; set; }

    /// <summary>
    /// 抵达最大轮数
    /// </summary>
    [JsonPropertyName("max_round_id")]
    public int MaxRoundId { get; set; }

    /// <summary>
    /// 纹章类型
    /// </summary>
    [JsonPropertyName("heraldry")]
    public int Heraldry { get; set; }

    /// <summary>
    /// 明星挑战星章数量
    /// </summary>
    [JsonPropertyName("medal_num")]
    public int MedalNum { get; set; }


    [JsonPropertyName("detail")]
    public ImaginariumTheaterDetail Detail { get; set; }


    [JsonPropertyName("stat")]
    public ImaginariumTheaterStat Stat { get; set; }


    [JsonPropertyName("schedule")]
    public ImaginariumTheaterSchedule Schedule { get; set; }


    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }


    [JsonPropertyName("has_detail_data")]
    public bool HasDetailData { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
