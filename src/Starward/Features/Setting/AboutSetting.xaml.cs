using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Features.Update;
using System;
using System.Threading.Tasks;


namespace Starward.Features.Setting;

[INotifyPropertyChanged]
public sealed partial class AboutSetting : UserControl
{


    private readonly ILogger<AboutSetting> _logger = AppConfig.GetLogger<AboutSetting>();


    public AboutSetting()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Unloaded += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);
    }




    /// <summary>
    /// 预览版
    /// </summary>
    public bool EnablePreviewRelease
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.EnablePreviewRelease = value;
            }
        }
    } = AppConfig.EnablePreviewRelease;


    /// <summary>
    /// 是最新版
    /// </summary>
    public bool IsUpdated { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 更新错误文本
    /// </summary>
    public string? UpdateErrorText { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// 检查更新
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            IsUpdated = false;
            UpdateErrorText = null;
            var release = await AppConfig.GetService<UpdateService>().CheckUpdateAsync(true);
            if (release != null)
            {
                new UpdateWindow { NewVersion = release }.Activate();
            }
            else
            {
                IsUpdated = true;
            }
        }
        catch (Exception ex)
        {
            UpdateErrorText = ex.Message;
            _logger.LogError(ex, "Check update");
        }
    }




}
