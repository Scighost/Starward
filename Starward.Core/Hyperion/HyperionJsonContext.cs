using Starward.Core.Hyperion.Genshin.SpiralAbyss;
using Starward.Core.Hyperion.Genshin.TravelersDiary;
using Starward.Core.Hyperion.StarRail.ForgottenHall;
using Starward.Core.Hyperion.StarRail.SimulatedUniverse;
using Starward.Core.Hyperion.StarRail.TrailblazeCalendar;
using System.Text.Json.Serialization;

namespace Starward.Core.Hyperion;


[JsonSerializable(typeof(MihoyoApiWrapper<HyperionUserWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<HyperionGameRoleWrapper>))]
[JsonSerializable(typeof(MihoyoApiWrapper<SpiralAbyssInfo>))]
[JsonSerializable(typeof(MihoyoApiWrapper<TravelersDiarySummary>))]
[JsonSerializable(typeof(MihoyoApiWrapper<TravelersDiaryDetail>))]
[JsonSerializable(typeof(MihoyoApiWrapper<TrailblazeCalendarSummary>))]
[JsonSerializable(typeof(MihoyoApiWrapper<TrailblazeCalendarDetail>))]
[JsonSerializable(typeof(MihoyoApiWrapper<ForgottenHallInfo>))]
[JsonSerializable(typeof(MihoyoApiWrapper<ForgottenHallTime>))]
[JsonSerializable(typeof(MihoyoApiWrapper<SimulatedUniverseInfo>))]
[JsonSerializable(typeof(MihoyoApiWrapper<SimulatedUniverseTime>))]
internal partial class HyperionJsonContext : JsonSerializerContext
{

}
