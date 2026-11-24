using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


// POST https://passport-api.mihoyo.com/account/ma-cn-session/app/createCrossLoginToken
/// <summary>
/// 国服获取跨端免密登录地址的请求体。
/// </summary>
public class CreateCrossLoginTokenRequest
{

    /// <summary>
    /// 会话 stoken。
    /// </summary>
    [JsonPropertyName("stoken")]
    public string Stoken { get; set; }


    /// <summary>
    /// 账号 mid。
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; }


    /// <summary>
    /// 账号数字 ID，接口要求字符串。
    /// </summary>
    [JsonPropertyName("aid")]
    public string Aid { get; set; }


    /// <summary>
    /// 目标地址。
    /// </summary>
    [JsonPropertyName("dest")]
    public string Dest { get; set; }

}



// POST https://passport-api-sg.hoyoverse.com/account/ma-passport/token/getCrossTokenUrlBySToken
/// <summary>
/// 国际服获取跨端免密登录地址的请求体。
/// </summary>
public class GetCrossTokenUrlBySTokenRequest
{

    /// <summary>
    /// 账号 mid。
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; }


    /// <summary>
    /// 会话 stoken。
    /// </summary>
    [JsonPropertyName("stoken")]
    public string Stoken { get; set; }


    /// <summary>
    /// 目标地址。
    /// </summary>
    [JsonPropertyName("dest")]
    public string Dest { get; set; }

}



/// <summary>
/// 跨端免密登录地址结果。
/// </summary>
public class CrossLoginTokenResult
{

    /// <summary>
    /// 带免密凭据的目标地址。
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

}
