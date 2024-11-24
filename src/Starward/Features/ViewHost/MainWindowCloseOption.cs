using Starward.Helpers.Enumeration;

namespace Starward.Features.ViewHost;

/// <summary>
/// 关闭主窗口的选项
/// </summary>
public enum MainWindowCloseOption
{

    /// <summary>
    /// 隐藏窗口
    /// </summary>
    [LocalizationKey(nameof(Lang.ExperienceSettingPage_MinimizeToSystemTray))]
    Hide = 1,

    /// <summary>
    /// 退出进程
    /// </summary>
    [LocalizationKey(nameof(Lang.ExperienceSettingPage_ExitCompletely))]
    Exit = 2,

}