using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Features.GameLauncher;


/// <summary>
/// 自定义启动预设。用户可以定义多个启动命令行，例如通过 FPS Unlocker、脚本等方式启动游戏。
/// 参考 <see href="https://github.com/Scighost/Starward/issues/1858"/>
/// </summary>
public sealed class GameLaunchScheme
{

    /// <summary>
    /// 唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");


    /// <summary>
    /// 显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;


    /// <summary>
    /// 启动程序的完整路径。为空时使用游戏本体的默认启动程序。
    /// </summary>
    public string? ExecutablePath { get; set; }


    /// <summary>
    /// 附加的命令行参数。
    /// </summary>
    public string? Arguments { get; set; }


    /// <summary>
    /// 需要以管理员权限运行。仅当 <see cref="ExecutablePath"/> 为 .exe 或 .bat 时有效。
    /// </summary>
    public bool RunAsAdmin { get; set; }


    /// <summary>
    /// 是否是默认（内置）预设。内置预设不可删除，且始终采用当前设置页的 “自定义启动程序” 与 “命令行参数” 值。
    /// </summary>
    [JsonIgnore]
    public bool IsBuiltIn { get; set; }


    /// <summary>
    /// 内置默认预设 Id
    /// </summary>
    public const string BuiltInDefaultId = "__default__";


    public GameLaunchScheme Clone()
    {
        return new GameLaunchScheme
        {
            Id = Id,
            Name = Name,
            ExecutablePath = ExecutablePath,
            Arguments = Arguments,
            RunAsAdmin = RunAsAdmin,
            IsBuiltIn = IsBuiltIn,
        };
    }


    internal static List<GameLaunchScheme> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<GameLaunchScheme>();
        }
        try
        {
            List<GameLaunchScheme>? list = JsonSerializer.Deserialize<List<GameLaunchScheme>>(json);
            if (list is null)
            {
                return new List<GameLaunchScheme>();
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                GameLaunchScheme item = list[i];
                if (string.IsNullOrEmpty(item.Id))
                {
                    item.Id = Guid.NewGuid().ToString("N");
                }
                // 用户不能通过配置构造出内置预设
                if (item.Id == BuiltInDefaultId)
                {
                    list.RemoveAt(i);
                }
            }
            return list;
        }
        catch
        {
            return new List<GameLaunchScheme>();
        }
    }


    internal static string? Serialize(List<GameLaunchScheme>? list)
    {
        if (list is null || list.Count == 0)
        {
            return null;
        }
        return JsonSerializer.Serialize(list, AppConfig.JsonSerializerOptions);
    }

}
