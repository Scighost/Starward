using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Hypergryph;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Starward.Features.GameSelector;

public sealed partial class LauncherConfigSettingDialog : ContentDialogBase
{

    private readonly ILogger<LauncherConfigSettingDialog> _logger = AppConfig.GetLogger<LauncherConfigSettingDialog>();

    private readonly HoYoPlayClient _hoYoPlayClient = AppConfig.GetService<HoYoPlayClient>();

    private readonly HoYoPlayBetaClient _hoYoPlayBetaClient = AppConfig.GetService<HoYoPlayBetaClient>();
    private readonly HypergryphLauncherClient _hypergryphLauncherClient = AppConfig.GetService<HypergryphLauncherClient>();
    private LauncherConfigModel? _originalConfig;


    public LauncherConfigSettingDialog() : base()
    {
        InitializeComponent();
    }


    public ObservableCollection<LauncherConfigModel> LauncherConfigs { get; set => SetProperty(ref field, value); } = new();

    public LauncherConfigModel? EditingConfig { get; set => SetProperty(ref field, value); }

    public string? ErrorText { get; set => SetProperty(ref field, value); }

    public bool EnabledLauncherChanged { get; set; }


    protected override void OnLoaded()
    {
        try
        {
            var list = LauncherConfigModel.GetEditable();
            list.Add(new() { IsAddButton = true });
            LauncherConfigs = new(list);
        }
        catch { }
    }



    private void PivotGoBack()
    {
        Pivot_Content.SelectedIndex = 0;
        ErrorText = null;
        EditingConfig = null;
        _originalConfig = null;
    }



    private void Button_AddConfig_Click(object sender, RoutedEventArgs e)
    {
        EditingConfig = new() { Region = (ServerRegion)(-1), Editable = true };
        Pivot_Content.SelectedIndex = 1;
    }


    private void Button_EditConfig_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: LauncherConfigModel { Editable: true } model })
        {
            string id = model.Id;
            if (model.Region == ServerRegion.Hypergryph && HypergryphGameConstants.GetEndfieldChannelGameId(id) is GameId gameId)
            {
                id = HypergryphGameConstants.GetGameProfile(gameId).GameAppCode;
            }
            _originalConfig = model;
            EditingConfig = new(id, model.Channel, model.SubChannel, model.Region, model.Name, model.Icon, model.SmallIcon,
                model.CanLogin, model.IsBeta) { Editable = true, IsSelected = model.IsSelected };
            Pivot_Content.SelectedIndex = 1;
        }
    }



    [RelayCommand]
    private void SaveEnabledLauncher()
    {
        try
        {
            string text = string.Join(',', LauncherConfigs.Where(x => x.IsSelected).Select(x => x.Id));
            AppConfig.EnabledLauncherIds = text;
            EnabledLauncherChanged = true;
            Close();
        }
        catch { }
    }



    #region Edit



    private async void Button_PickIcon_Click(object sender, RoutedEventArgs e)
    {
        string? file = await FileDialogHelper.PickSingleFileAsync(XamlRoot, ("Image", ".png"), ("Image", ".jpg"), ("Image", ".ico"));
        if (File.Exists(file))
        {
            EditingConfig?.Icon = file;
        }
    }



    private async void Button_PickSmallIcon_Click(object sender, RoutedEventArgs e)
    {
        string? file = await FileDialogHelper.PickSingleFileAsync(XamlRoot, ("Image", ".png"), ("Image", ".jpg"), ("Image", ".ico"));
        if (File.Exists(file))
        {
            EditingConfig?.SmallIcon = file;
        }
    }



    private static string CopyImageToUserDataFolder(string file)
    {
        if (!File.Exists(file))
        {
            return file;
        }
        string folder = Path.Join(AppConfig.UserDataFolder, "image");
        if (string.Equals(Path.GetDirectoryName(Path.GetFullPath(file)), Path.GetFullPath(folder), StringComparison.OrdinalIgnoreCase))
        {
            return file;
        }
        Directory.CreateDirectory(folder);
        string hash = Convert.ToHexStringLower(XxHash3.Hash(File.ReadAllBytes(file)));
        string name = $"{hash}{Path.GetExtension(file).ToLowerInvariant()}";
        string target = Path.Join(folder, Path.GetFileName(file));
        if (!File.Exists(target))
        {
            File.Copy(file, target);
        }
        return target;
    }


    private CancellationTokenSource? _saveTokenSource;


    [RelayCommand]
    private async Task SaveEditConfigAsync()
    {
        try
        {
            ErrorText = null;
            _saveTokenSource?.Cancel();
            _saveTokenSource = new();
            CancellationToken token = _saveTokenSource.Token;

            if (EditingConfig is not LauncherConfigModel config)
            {
                PivotGoBack();
                return;
            }

            if (string.IsNullOrWhiteSpace(config.Id) || string.IsNullOrWhiteSpace(config.Name)
                || config.Region is not (ServerRegion.HoYoverse or ServerRegion.miHoYo or ServerRegion.Hypergryph))
            {
                ErrorText = Lang.LauncherConfigSettingDialog_IDNameAndRegionCannotBeEmpty;
                return;
            }

            string id = config.Id.Trim();
            if (config.Region == ServerRegion.Hypergryph)
            {
                GameId? gameId = HypergryphGameConstants.CreateEndfieldChannelGameId(id, config.Channel, config.SubChannel);
                if (gameId?.GameBiz.Value != GameBiz.endfield_channel)
                {
                    ErrorText = Lang.LauncherConfigSettingDialog_EndfieldChannelInvalid;
                    return;
                }
                id = gameId.Id;
                if (!await _hypergryphLauncherClient.HasEndfieldPackageAsync(gameId, token))
                {
                    ErrorText = Lang.LauncherConfigSettingDialog_NoGamesFoundForThisLauncher;
                    return;
                }
                config.IsBeta = false;
                config.CanLogin = false;
                config.Icon = string.IsNullOrWhiteSpace(config.Icon) ? HypergryphGameConstants.HypergryphLogo : config.Icon;
                config.SmallIcon = string.IsNullOrWhiteSpace(config.SmallIcon) ? HypergryphGameConstants.HypergryphLogo : config.SmallIcon;
            }
            else
            {
                IHoYoPlayClient client = config.IsBeta ? _hoYoPlayBetaClient : _hoYoPlayClient;
                client.LauncherConfig = config.ToLauncherConfig();
                if ((await client.GetGameInfoAsync(token)).Count is 0)
                {
                    ErrorText = Lang.LauncherConfigSettingDialog_NoGamesFoundForThisLauncher;
                    return;
                }
            }
            token.ThrowIfCancellationRequested();
            if (LauncherConfigs.Any(x => x != _originalConfig && string.Equals(x.Id, id, StringComparison.Ordinal)))
            {
                ErrorText = Lang.LauncherConfigSettingDialog_ChannelAlreadyExists;
                return;
            }
            config.Id = id;
            config.Icon = CopyImageToUserDataFolder(config.Icon);
            config.SmallIcon = CopyImageToUserDataFolder(config.SmallIcon);

            if (_originalConfig is null)
            {
                config.IsSelected = config.Region == ServerRegion.Hypergryph;
                LauncherConfigs.Insert(LauncherConfigs.Count - 1, config);
            }
            else
            {
                LauncherConfigs[LauncherConfigs.IndexOf(_originalConfig)] = config;
            }

            LauncherConfigModel.SaveExtra(LauncherConfigs.Where(x => x.Editable));
            EnabledLauncherChanged = true;
            PivotGoBack();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save config for launcher ({Id}, {Region})", EditingConfig?.Id, EditingConfig?.Region);
            ErrorText = ex.Message;
        }
    }



    [RelayCommand]
    private void CancelEditConfig()
    {
        try
        {
            _saveTokenSource?.Cancel();
            EditingConfig = null;
            PivotGoBack();
        }
        catch { }
    }


    [RelayCommand]
    private void DeleteEditConfig()
    {
        try
        {
            _saveTokenSource?.Cancel();
            if (_originalConfig is not null)
            {
                LauncherConfigs.Remove(_originalConfig);
                LauncherConfigModel.SaveExtra(LauncherConfigs.Where(x => x.Editable));
                EnabledLauncherChanged = true;
            }
            PivotGoBack();
        }
        catch { }
    }



    #endregion



}
