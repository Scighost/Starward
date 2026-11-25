using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


// POST https://sg-public-api.hoyoverse.com/account/ma-passport/token/getBySToken
public class GetBySTokenRequest
{

    [JsonPropertyName("dst_token_types")]
    public List<TokenType> DestTokenTypes { get; set; } = [];

}


public class GetBySTokenResponse
{
    [JsonPropertyName("tokens")]
    public List<PassportToken> Tokens { get; set; } = [];

    [JsonPropertyName("user_info")]
    public UserInfo UserInfo { get; set; }

    public string? GetToken(TokenType tokenType)
    {
        return Tokens?.FirstOrDefault(x => x.TokenType == tokenType)?.Token;
    }
}
