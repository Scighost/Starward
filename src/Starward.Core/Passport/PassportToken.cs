using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


public class PassportToken
{
    [JsonPropertyName("token_type")]
    public TokenType TokenType { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; }
}
