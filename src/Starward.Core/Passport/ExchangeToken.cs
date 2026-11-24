using System.Text.Json.Serialization;

namespace Starward.Core.Passport;

public class ExchangeTokenRequest
{
    [JsonPropertyName("dst_token_type")]
    public TokenType DestTokenType { get; set; }

    [JsonPropertyName("mid")]
    public string Mid { get; set; }

    [JsonPropertyName("src_token")]
    public PassportToken SourceToken { get; set; }

    public static ExchangeTokenRequest FromStoken(string stoken, string mid)
    {
        return new ExchangeTokenRequest { SourceToken = new PassportToken { TokenType = TokenType.SToken, Token = stoken }, Mid = mid };
    }
}


public class ExchangeTokenResponse
{

    [JsonPropertyName("token")]
    public PassportToken Token { get; set; }

    [JsonPropertyName("user_info")]
    public UserInfo UserInfo { get; set; }

    [JsonPropertyName("expire_in_seconds")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int ExpireInSeconds { get; set; }
}