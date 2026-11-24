using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


// POST https://passport-api.mihoyo.com/account/ma-cn-session/app/verify
/// <summary>
/// 国服会话校验请求体。
/// </summary>
public class SessionVerifyRequest
{

    /// <summary>
    /// 会话 token。
    /// </summary>
    [JsonPropertyName("token")]
    public PassportToken Token { get; set; }


    /// <summary>
    /// 是否刷新 token。
    /// </summary>
    [JsonPropertyName("refresh")]
    public bool Refresh { get; set; }


    /// <summary>
    /// 账号 mid。
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; }

}



/// <summary>
/// 会话校验结果，国服与国际服同构，刷新后的 token 可能为空。
/// </summary>
public class SessionVerifyResult
{

    /// <summary>
    /// 国服 verify 返回的刷新后 token；国际服 verifySToken 不返回该字段。
    /// </summary>
    [JsonPropertyName("new_token")]
    public PassportToken? NewToken { get; set; }


    /// <summary>
    /// 国际服 verifySToken 返回的 token 列表，通常只有一项。
    /// </summary>
    [JsonPropertyName("tokens")]
    public List<PassportToken>? Tokens { get; set; }


    /// <summary>
    /// 账号信息。
    /// </summary>
    [JsonPropertyName("user_info")]
    public UserInfo UserInfo { get; set; }


    /// <summary>
    /// 刷新后的 stoken。国服取 <c>new_token</c>，国际服取 <c>tokens</c> 中第一个 stoken。
    /// </summary>
    public string? NewSToken => !string.IsNullOrWhiteSpace(NewToken?.Token)
                                ? NewToken.Token
                                : Tokens?.FirstOrDefault(x => x.TokenType == TokenType.SToken && !string.IsNullOrWhiteSpace(x.Token))?.Token;

}



// POST https://passport-api-sg.hoyoverse.com/account/ma-passport/token/verifySToken
/// <summary>
/// 国际服会话校验请求体。
/// </summary>
public class VerifySTokenRequest
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

}



// POST https://passport-api.mihoyo.com/account/ma-cn-session/app/logout
// POST https://passport-api-sg.hoyoverse.com/account/ma-passport/api/logout
/// <summary>
/// 登出请求体，国服与国际服相同。
/// </summary>
public class LogoutRequest
{

    /// <summary>
    /// 账号 mid。
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; }


    /// <summary>
    /// 会话 token。
    /// </summary>
    [JsonPropertyName("token")]
    public PassportToken Token { get; set; }

}



/// <summary>
/// 登出结果。前端只关心请求是否成功，不读取响应字段。
/// </summary>
public class LogoutResult
{

}
