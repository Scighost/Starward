using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;


namespace Starward.Features.GameSelector;

/// <summary>
/// 将 <see cref="MediaTransportControls"/> 模板中自带的 <c>TimeElapsedElement</c>（已播放，形如 0:00:05）
/// 和 <c>TimeRemainingElement</c>（剩余，形如 0:00:10）文本转换为 "已播放 / 全部"（m:ss）显示在 TextBlock 上。
/// <para>
/// MediaTransportControls 的内部逻辑会持续更新这两个文本，把它们的 Text 绑定到本附加属性上，
/// 显示就会随控件自动刷新，无需在页面代码中订阅 PlaybackSession 的事件。用法：
/// <code>
/// &lt;TextBlock local:MediaTimeText.ElapsedSource="{Binding Text, ElementName=TimeElapsedElement}"
///            local:MediaTimeText.RemainingSource="{Binding Text, ElementName=TimeRemainingElement}" /&gt;
/// </code>
/// 全部时长由已播放 + 剩余相加得到（MediaTransportControls 不直接暴露总时长）。
/// </para>
/// </summary>
public sealed class MediaTimeText
{
    /// <summary>
    /// 已播放时间文本的来源（MediaTransportControls 模板中的 TimeElapsedElement.Text，形如 0:00:05）
    /// </summary>
    public static readonly DependencyProperty ElapsedSourceProperty = DependencyProperty.RegisterAttached(
        "ElapsedSource", typeof(string), typeof(MediaTimeText), new PropertyMetadata(null, OnSourceChanged));


    public static string? GetElapsedSource(DependencyObject element) => (string?)element.GetValue(ElapsedSourceProperty);

    public static void SetElapsedSource(DependencyObject element, string? value) => element.SetValue(ElapsedSourceProperty, value);



    /// <summary>
    /// 剩余时间文本的来源（MediaTransportControls 模板中的 TimeRemainingElement.Text，形如 0:00:10）
    /// </summary>
    public static readonly DependencyProperty RemainingSourceProperty = DependencyProperty.RegisterAttached(
        "RemainingSource", typeof(string), typeof(MediaTimeText), new PropertyMetadata(null, OnSourceChanged));


    public static string? GetRemainingSource(DependencyObject element) => (string?)element.GetValue(RemainingSourceProperty);

    public static void SetRemainingSource(DependencyObject element, string? value) => element.SetValue(RemainingSourceProperty, value);



    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            textBlock.Text = FormatTimeText(GetElapsedSource(textBlock), GetRemainingSource(textBlock));
        }
    }


    /// <summary>
    /// 已播放 + 剩余 = 全部，格式为 m:ss
    /// </summary>
    private static string FormatTimeText(string? elapsedSource, string? remainingSource)
    {
        if (TryParseTime(elapsedSource, out TimeSpan elapsed) && TryParseTime(remainingSource, out TimeSpan remaining))
        {
            TimeSpan total = elapsed + remaining.Duration();
            return $"{ToTimeString(elapsed)} / {ToTimeString(total)}";
        }
        return "- / -";
    }


    private static string ToTimeString(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
        {
            time = TimeSpan.Zero;
        }
        return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
    }


    /// <summary>
    /// 解析形如 "0:00:05" / "05:00" 的时间文本（MediaTransportControls 固定使用 h:mm:ss，也可能带负号）
    /// </summary>
    private static bool TryParseTime(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }
        text = text.Trim();
        bool negative = text.StartsWith('-');
        if (negative)
        {
            text = text[1..];
        }
        string[] parts = text.Split(':');
        if (parts.Length is not (2 or 3))
        {
            return false;
        }
        if (!int.TryParse(parts[^1], NumberStyles.None, CultureInfo.InvariantCulture, out int seconds)
            || !int.TryParse(parts[^2], NumberStyles.None, CultureInfo.InvariantCulture, out int minutes))
        {
            return false;
        }
        int hours = 0;
        if (parts.Length == 3 && !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hours))
        {
            return false;
        }
        time = new TimeSpan(hours, minutes, seconds);
        if (negative)
        {
            time = time.Negate();
        }
        return true;
    }
}
