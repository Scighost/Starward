using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class ColorfulTextBlock : UserControl
{


    public ColorfulTextBlock()
    {
        this.InitializeComponent();
    }




    public TextWrapping TextWrapping
    {
        get { return (TextWrapping)GetValue(TextWrappingProperty); }
        set { SetValue(TextWrappingProperty, value); }
    }

    // Using a DependencyProperty as the backing store for TextWrapping.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty TextWrappingProperty =
        DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(ColorfulTextBlock), new PropertyMetadata(default));



    [ObservableProperty]
    private string _text;
    partial void OnTextChanged(string value)
    {
        try
        {
            var text = ThisTextBlock;
            text.Inlines.Clear();
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }
            var desc = value.AsSpan();
            int lastIndex = 0;
            for (int i = 0; i < desc.Length; i++)
            {
                // 换行
                if (desc[i] == '\\' && desc[i + 1] == 'n')
                {
                    text.Inlines.Add(new Run { Text = desc[lastIndex..i].ToString() });
                    text.Inlines.Add(new LineBreak());
                    i += 1;
                    lastIndex = i + 1;
                }
                // 颜色
                if (desc[i] == '<' && desc[i + 1] == 'c')
                {
                    text.Inlines.Add(new Run { Text = desc[lastIndex..i].ToString() });
                    var colorLength = desc.Slice(i + 8).IndexOf('>');
                    var colorString = desc.Slice(i + 8, colorLength);
                    var color = Convert.FromHexString(colorString);
                    var textLength = desc.Slice(i + 9 + colorLength).IndexOf('<');
                    if (colorLength == 8)
                    {
                        text.Inlines.Add(new Run
                        {
                            Text = desc.Slice(i + 9 + colorLength, textLength).ToString(),
                            Foreground = new SolidColorBrush(Color.FromArgb(color[3], color[0], color[1], color[2])),
                        });
                    }
                    else if (colorLength == 6)
                    {
                        text.Inlines.Add(new Run
                        {
                            Text = desc.Slice(i + 9 + colorLength, textLength).ToString(),
                            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, color[0], color[1], color[2])),
                        });
                    }
                    else
                    {
                        text.Inlines.Add(new Run
                        {
                            Text = desc.Slice(i + 9 + colorLength, textLength).ToString(),
                        });
                    }
                    i += 16 + colorLength + textLength;
                    lastIndex = i + 1;
                }
                // 引用
                if (desc[i] == '<' && desc[i + 1] == 'i')
                {
                    text.Inlines.Add(new Run { Text = desc[lastIndex..i].ToString() });
                    var length = desc.Slice(i + 3).IndexOf('<');
                    text.Inlines.Add(new Run
                    {
                        Text = desc.Slice(i + 3, length).ToString(),
                        FontStyle = Windows.UI.Text.FontStyle.Italic,
                    });
                    i += length + 6;
                    lastIndex = i + 1;
                }
                // 结尾
                if (i == desc.Length - 1)
                {
                    text.Inlines.Add(new Run { Text = desc.Slice(lastIndex).ToString() });
                }
            }
        }
        catch (Exception ex)
        {
            ThisTextBlock.Inlines.Clear();
            ThisTextBlock.Text = value;
        }
    }






}
