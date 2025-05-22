namespace Starward.Features.GameSelector;

/// <summary>
/// 主窗口可拖动矩形适应游戏图标
/// </summary>
internal class MainWindowDragRectAdaptToGameIconMessage
{

    public bool IgnoreDpiChanged { get; set; }


    public MainWindowDragRectAdaptToGameIconMessage(bool ignoreDpiChanged = false)
    {
        IgnoreDpiChanged = ignoreDpiChanged;
    }

}