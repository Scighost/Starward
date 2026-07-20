using CommunityToolkit.Mvvm.ComponentModel;

namespace Starward.Features.GameLauncher;


/// <summary>
/// 启动预设列表 UI 包装类：将 <see cref="GameLaunchScheme"/> 数据与设置对话框中显示相关的
/// 本地化字符串 / 只读状态 / 输入控件文本等聚合为一个可绑定实体，简化 XAML DataTemplate。
/// </summary>
public sealed partial class LaunchSchemeItem : ObservableObject
{

    public LaunchSchemeItem(GameLaunchScheme scheme, string? defaultArgumentsHint)
    {
        Scheme = scheme;
        _defaultArgumentsHint = defaultArgumentsHint;
    }


    public GameLaunchScheme Scheme { get; }


    private string? _defaultArgumentsHint;


    public bool IsBuiltIn => Scheme.IsBuiltIn;


    public string Name
    {
        get => Scheme.Name;
        set
        {
            if (Scheme.Name != value)
            {
                Scheme.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }


    public string? ExecutablePath
    {
        get => Scheme.ExecutablePath;
        set
        {
            if (Scheme.ExecutablePath != value)
            {
                Scheme.ExecutablePath = value;
                OnPropertyChanged();
            }
        }
    }


    public string? Arguments
    {
        get => Scheme.Arguments;
        set
        {
            if (Scheme.Arguments != value)
            {
                Scheme.Arguments = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayArguments));
            }
        }
    }


    public bool RunAsAdmin
    {
        get => Scheme.RunAsAdmin;
        set
        {
            if (Scheme.RunAsAdmin != value)
            {
                Scheme.RunAsAdmin = value;
                OnPropertyChanged();
            }
        }
    }


    /// <summary>
    /// 内置预设显示的名称固定为本地化的 “默认”。
    /// </summary>
    public string DisplayName => IsBuiltIn ? Lang.StartGameButton_DefaultLaunchOption : Scheme.Name;


    /// <summary>
    /// 内置预设的命令行参数直接展示当前设置页面上方的默认命令行参数，供用户参考，不可编辑。
    /// </summary>
    public string? DisplayArguments => IsBuiltIn ? _defaultArgumentsHint : Scheme.Arguments;


    public string NameHeader => Lang.GameLauncherSettingDialog_LaunchOptionName;

    public string NamePlaceholder => IsBuiltIn ? Lang.StartGameButton_DefaultLaunchOption : Lang.GameLauncherSettingDialog_LaunchOptionName;

    public string ExecutableHeader => Lang.GameLauncherSettingDialog_LaunchOptionExecutable;

    public string ArgumentsHeader => Lang.GameLauncherSettingDialog_LaunchOptionArguments;

    public string RunAsAdminText => Lang.GameLauncherSettingDialog_LaunchOptionRunAsAdmin;

    public string DefaultTooltip => Lang.GameLauncherSettingDialog_LaunchOptionDefaultTooltip;

    public string MoveUpTooltip => Lang.GameLauncherSettingDialog_LaunchOptionMoveUp;

    public string MoveDownTooltip => Lang.GameLauncherSettingDialog_LaunchOptionMoveDown;


    /// <summary>
    /// 当上方 “命令行参数” 文本框内容发生变化时，重新刷新内置预设显示。
    /// </summary>
    public void RefreshDefaultArgumentsHint(string? hint)
    {
        if (_defaultArgumentsHint != hint)
        {
            _defaultArgumentsHint = hint;
            if (IsBuiltIn)
            {
                OnPropertyChanged(nameof(DisplayArguments));
            }
        }
    }

}
