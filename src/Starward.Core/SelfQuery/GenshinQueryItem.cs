using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;

public class GenshinQueryItem : IJsonOnDeserialized
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonPropertyName("add_num")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long AddNum { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("datetime")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("type")]
    public GenshinQueryType Type { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("level")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Level { get; set; }

    [JsonPropertyName("quality")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Quality { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime Time { get; set; }

    public void OnDeserialized()
    {
        if (DateTime == default && Time != default)
        {
            DateTime = Time;
        }
    }
}


/// <summary>
/// 装扮部件获取
/// </summary>
public class GenshinSelfQueryItem_BeyondCostume
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime Time { get; set; }

    [JsonPropertyName("source_type")]
    public string SourceType { get; set; }

    [JsonPropertyName("body_type")]
    public string BodyType { get; set; }

    [JsonPropertyName("costume_level")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int CostumeLevel { get; set; }

    [JsonPropertyName("costume_name")]
    public string CostumeName { get; set; }

    /// <summary>
    /// 试用
    /// </summary>
    [JsonPropertyName("is_trial")]
    public bool IsTrial { get; set; }

    [JsonPropertyName("trial_expire_time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime TrialExpireTime { get; set; }
}


/// <summary>
/// 装扮套装获取
/// </summary>
public class GenshinSelfQueryItem_BeyondCostumeSuit
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime Time { get; set; }

    [JsonPropertyName("source_type")]
    public string SourceType { get; set; }

    [JsonPropertyName("body_type")]
    public string BodyType { get; set; }

    [JsonPropertyName("costume_suit_level")]
    public int CostumeSuitLevel { get; set; }

    [JsonPropertyName("costume_suit_name")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public string CostumeSuitName { get; set; }

    /// <summary>
    /// 试用
    /// </summary>
    [JsonPropertyName("is_trial")]
    public bool IsTrial { get; set; }

    [JsonPropertyName("trial_expire_time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime TrialExpireTime { get; set; }
}


/// <summary>
/// 千星奇域纪游
/// </summary>
public class GenshinSelfQueryItem_BeyondBattlePath
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]

    public DateTime Time { get; set; }

    [JsonPropertyName("unlock_type")]
    public string UnlockType { get; set; }

    [JsonPropertyName("unlock_reason")]
    public string UnlockReason { get; set; }
}

/// <summary>
/// 纪行
/// </summary>
public class GenshinSelfQueryItem_BattlePath
{
    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("sub_action_name")]
    public string SubActionName { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("datetime")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime Datetime { get; set; }

    [JsonPropertyName("battle_path_type")]
    public string BattlePathType { get; set; }
}