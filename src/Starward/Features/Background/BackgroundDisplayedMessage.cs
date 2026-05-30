using Starward.Core.HoYoPlay;

namespace Starward.Features.Background;

/// <summary>
/// 由 <see cref="AppBackground"/> 在实际显示某个背景后发送，
/// 通知启动器页面同步播放/暂停按钮等与当前背景相关的状态。
/// </summary>
internal class BackgroundDisplayedMessage
{

    public GameBackground? GameBackground { get; set; }

    public BackgroundDisplayedMessage(GameBackground? gameBackground = null)
    {
        GameBackground = gameBackground;
    }

}
