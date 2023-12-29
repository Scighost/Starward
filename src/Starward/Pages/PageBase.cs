using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Messages;

namespace Starward.Pages;

public abstract class PageBase : Page
{


    public GameBiz CurrentGameBiz { get; private set; }


    public PageBase()
    {
        Loaded += PageEx_Loaded;
        Unloaded += PageEx_Unloaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => OnLanguageChanged());
    }



    private void PageEx_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        OnLoaded();
    }


    private void PageEx_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Loaded -= PageEx_Loaded;
        Unloaded -= PageEx_Unloaded;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        OnUnloaded();
    }



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is GameBiz biz)
        {
            CurrentGameBiz = biz;
        }
    }



    protected virtual void OnLoaded()
    {

    }



    protected virtual void OnUnloaded()
    {

    }



    protected virtual void OnLanguageChanged()
    {

    }


}


