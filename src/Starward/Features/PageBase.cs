using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System.Threading;

namespace Starward.Features;

[INotifyPropertyChanged]
public abstract partial class PageBase : Page
{

    public GameId CurrentGameId { get; protected set => SetProperty(ref field, value); }

    public GameBiz CurrentGameBiz { get; protected set => SetProperty(ref field, value); }

    private readonly CancellationTokenSource UnloadCancellationTokenSource = new();

    protected CancellationToken UnloadCancellationToken => UnloadCancellationTokenSource.Token;


    public PageBase()
    {
        Loaded += PageEx_Loaded;
        Unloaded += PageEx_Unloaded;
    }



    private void PageEx_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OnLoaded();
    }


    private void PageEx_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= PageEx_Loaded;
        Unloaded -= PageEx_Unloaded;
        OnUnloaded();
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is GameId id)
        {
            CurrentGameId = id;
            CurrentGameBiz = id.GameBiz;
        }
        else if (e.Parameter is GameBiz biz)
        {
            CurrentGameBiz = biz;
            CurrentGameId = GameId.FromGameBiz(biz)!;
        }
    }


    protected virtual void OnLoaded()
    {

    }


    protected virtual void OnUnloaded()
    {

    }


    protected void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize -= 1;
        }
    }

}
