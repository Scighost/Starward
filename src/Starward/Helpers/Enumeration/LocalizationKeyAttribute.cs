using System;


namespace Starward.Helpers.Enumeration;


/// <summary>
/// 与本地化语言的 Key 相关联
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class LocalizationKeyAttribute : Attribute
{
    public string Key { get; set; }

    public LocalizationKeyAttribute(string key)
    {
        Key = key;
    }
}