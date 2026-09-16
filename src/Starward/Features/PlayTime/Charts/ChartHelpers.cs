using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Starward.Features.PlayTime;

/// <summary>
/// 图表共享的资源辅助方法
/// </summary>
internal static class ChartHelpers
{

    /// <summary>
    /// 获取主题资源画刷
    /// </summary>
    public static T GetResource<T>(string key) where T : class
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is T t)
        {
            return t;
        }
        throw new KeyNotFoundException($"Theme resource '{key}' not found.");
    }

}
