using Starward.Core.GameRecord;
using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


[JsonSerializable(typeof(miHoYoApiWrapper<SessionVerifyResult>))]
[JsonSerializable(typeof(miHoYoApiWrapper<LogoutResult>))]
[JsonSerializable(typeof(miHoYoApiWrapper<CrossLoginTokenResult>))]
[JsonSerializable(typeof(miHoYoApiWrapper<AuthTicketResult>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GameRecordUserWrapper>))]
[JsonSerializable(typeof(miHoYoApiWrapper<GameRecordRoleWrapper>))]
[JsonSerializable(typeof(SessionVerifyRequest))]
[JsonSerializable(typeof(VerifySTokenRequest))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(CreateAuthTicketByGameBizRequest))]
[JsonSerializable(typeof(CreateAuthTicketBySTokenRequest))]
[JsonSerializable(typeof(CreateCrossLoginTokenRequest))]
[JsonSerializable(typeof(GetCrossTokenUrlBySTokenRequest))]
[JsonSerializable(typeof(ExchangeTokenRequest))]
[JsonSerializable(typeof(miHoYoApiWrapper<ExchangeTokenResponse>))]
[JsonSerializable(typeof(GetBySTokenRequest))]
[JsonSerializable(typeof(miHoYoApiWrapper<GetBySTokenResponse>))]
internal partial class PassportJsonContext : JsonSerializerContext
{

}
