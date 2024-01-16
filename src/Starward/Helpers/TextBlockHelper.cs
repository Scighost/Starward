using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.System;

namespace Starward.Helpers;

internal static class TextBlockHelper
{


    public static unsafe void Inlines(InlineCollection inlines, ReadOnlySpan<char> template, params (string Placeholder, string? Hyperlink)[] accentTexts)
    {
        try
        {
            ReadOnlySpan<char> str = template;
            Span<int> indexes = stackalloc int[accentTexts.Length];
            Brush? brush = Application.Current.Resources["AccentTextFillColorPrimaryBrush"] as Brush;
            while (str.Length > 0)
            {
                for (int i = 0; i < accentTexts.Length; i++)
                {
                    indexes[i] = str.IndexOf(accentTexts[i].Placeholder);
                }
                int j = GetFirstIndex(indexes);
                if (j < 0)
                {
                    inlines.Add(new Run { Text = str.ToString() });
                    break;
                }
                else
                {
                    int first = indexes[j];
                    (string placeholder, string? hyperlink) = accentTexts[j];
                    string replace = placeholder.Trim('{', '}');
                    if (first > 0)
                    {
                        inlines.Add(new Run { Text = str[..first].ToString() });
                        str = str[first..];
                    }
                    if (string.IsNullOrWhiteSpace(hyperlink))
                    {
                        inlines.Add(new Run { Text = replace, Foreground = brush });
                    }
                    else
                    {
                        Uri.TryCreate(hyperlink, UriKind.RelativeOrAbsolute, out var uri);
                        var hyper = new Hyperlink { NavigateUri = uri, UnderlineStyle = UnderlineStyle.None };
                        if (uri is not null && uri.Scheme is not "http" and not "https")
                        {
                            hyper.Click += (s, e) => _ = Launcher.LaunchUriAsync(uri);
                        }
                        hyper.Inlines.Add(new Run { Text = replace, Foreground = brush });
                        inlines.Add(hyper);
                    }
                    str = str[placeholder.Length..];
                }
            }
        }
        catch { }
    }



    private static int GetFirstIndex(Span<int> indexes)
    {
        int result = -1;
        int max = int.MaxValue;
        for (int i = 0; i < indexes.Length; i++)
        {
            if (indexes[i] >= 0 && indexes[i] < max)
            {
                result = i;
                max = indexes[i];
            }
        }
        return result;
    }


}
