using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


// POST https://passport-api.mihoyo.com/account/ma-cn-verifier/app/createAuthTicketByGameBiz
/// <summary>
/// 国服用 stoken 换取游戏登录票据的请求体。
/// </summary>
public class CreateAuthTicketByGameBizRequest
{
    [JsonPropertyName("game_biz")]
    public string GameBiz { get; set; }

    [JsonPropertyName("stoken")]
    public string Stoken { get; set; }

    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    [JsonPropertyName("mid")]
    public string Mid { get; set; }
}


// POST https://passport-api-sg.hoyoverse.com/account/ma-verifier/api/createAuthTicketBySToken
/// <summary>
/// 国际服用 stoken 换取游戏登录票据的请求体。
/// </summary>
public class CreateAuthTicketBySTokenRequest
{
    [JsonPropertyName("mid")]
    public string Mid { get; set; }

    [JsonPropertyName("stoken")]
    public string Stoken { get; set; }

    [JsonPropertyName("biz_name")]
    public string BizName { get; set; }
}


/// <summary>
/// 游戏登录票据结果。国服只返回 ticket，国际服还会返回 user_info。
/// </summary>
public class AuthTicketResult
{
    [JsonPropertyName("ticket")]
    public string Ticket { get; set; }

    [JsonPropertyName("user_info")]
    public UserInfo? UserInfo { get; set; }
}
