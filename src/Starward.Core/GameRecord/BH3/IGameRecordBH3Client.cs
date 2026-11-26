using Starward.Core.GameRecord.BH3.DailyNote;

namespace Starward.Core.GameRecord.BH3;

public interface IGameRecordBH3Client : IGameRecordClient
{

    /// <summary>
    /// 崩坏3实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<BH3DailyNote> GetBH3DailyNoteAsync(GameRecordRole role, CancellationToken cancellationToken = default);

}
