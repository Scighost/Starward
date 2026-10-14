using Starward.Core;

namespace Starward.Features.GameLauncher;


/// <summary>
/// 通知启动预设列表发生变化。
/// </summary>
/// <param name="GameBiz">发生变化的游戏区服；如为 <see cref="GameBiz.None"/> 则代表全部游戏。</param>
public sealed record LaunchSchemesChangedMessage(GameBiz GameBiz);
