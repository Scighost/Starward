using Starward.Core.GameRecord;

namespace Starward.Messages;

public record VerifyAccountMessage(GameRecordRole GameRole, string TargetUrl);
