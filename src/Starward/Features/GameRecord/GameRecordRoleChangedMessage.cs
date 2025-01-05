using Starward.Core.GameRecord;

namespace Starward.Features.GameRecord;

internal class GameRecordRoleChangedMessage
{

    public GameRecordRole? GameRole { get; set; }

    public GameRecordRoleChangedMessage(GameRecordRole? gameRole)
    {
        GameRole = gameRole;
    }

}
