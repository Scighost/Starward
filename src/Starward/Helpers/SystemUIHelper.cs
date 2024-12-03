using Vanara.PInvoke;

namespace Starward.Helpers;

public static class SystemUIHelper
{


    /// <summary>
    /// 辅助功能 - 视觉效果 - 透明效果
    /// </summary>
    public static bool TransparencyEffectEnabled
    {
        get
        {
            User32.SystemParametersInfo(User32.SPI.SPI_GETDISABLEOVERLAPPEDCONTENT, out bool enabled);
            return enabled;
        }
        set => User32.SystemParametersInfo(User32.SPI.SPI_SETDISABLEOVERLAPPEDCONTENT, value);
    }



    /// <summary>
    /// 辅助功能 - 视觉效果 - 动画效果
    /// </summary>
    public static bool AnimationEffectEnabled
    {
        get
        {
            User32.SystemParametersInfo(User32.SPI.SPI_GETCLIENTAREAANIMATION, out bool enabled);
            return enabled;
        }
        set => User32.SystemParametersInfo(User32.SPI.SPI_SETCLIENTAREAANIMATION, value);
    }



}
