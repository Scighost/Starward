namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakRecordMob
{
    public ChallengePeakRecordMob(ChallengePeakMobInfo mobInfo, ChallengePeakMobRecord? mobRecord)
    {
        MobInfo = mobInfo;
        MobRecord = mobRecord;
    }

    public ChallengePeakMobInfo MobInfo { get; set; }

    public ChallengePeakMobRecord? MobRecord { get; set; }


    public bool HasChallengeRecord => MobRecord?.HasChallengeRecord ?? false;

}
