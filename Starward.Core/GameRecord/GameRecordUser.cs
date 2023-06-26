using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord;

public class GameRecordUser
{

    /// <summary>
    /// 通行证 ID
    /// </summary>
    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("is_hoyolab")]
    public bool IsHoyolab { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// 介绍
    /// </summary>
    [JsonPropertyName("introduce")]
    public string? Introduce { get; set; }

    /// <summary>
    /// 头像 ID
    /// </summary>
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    /// <summary>
    /// 性别 0/1/2
    /// </summary>
    [JsonPropertyName("gender")]
    public int Gender { get; set; }

    /// <summary>
    /// 头像     
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 头像框    
    /// </summary>
    [JsonPropertyName("pendant")]
    public string? Pendant { get; set; }

    /// <summary>
    /// 与此账号相关联的 Cookie
    /// </summary>
    [JsonIgnore]
    public string? Cookie { get; set; }

}


internal class GameRecordUserWrapper
{
    [JsonPropertyName("user_info")]
    public GameRecordUser User { get; set; }
}