using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Features.GameSelector;

public partial class LauncherConfigModel : ObservableObject
{

    public string Id { get; set => SetProperty(ref field, value); }

    public int Channel { get; set => SetProperty(ref field, value); }

    public int SubChannel { get; set => SetProperty(ref field, value); }

    public ServerRegion Region { get; set => SetProperty(ref field, value); }

    public string Name { get; set => SetProperty(ref field, value); }

    public string Icon { get; set => SetProperty(ref field, value); }

    public string SmallIcon { get; set => SetProperty(ref field, value); }

    public bool CanLogin { get; set => SetProperty(ref field, value); }

    public bool IsBeta
    {
        get; set
        {
            SetProperty(ref field, value);
            if (value)
            {
                CanLogin = true;
            }
        }
    }


    [JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeatureText))]
    public partial bool Editable { get; set; }

    [JsonIgnore]
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeatureText))]
    public partial bool IsSelected { get; set; }

    [JsonIgnore]
    public string FeatureText => (CanLogin, IsBeta) switch
    {
        (_, true) => Lang.LauncherConfigSettingDialog_BetaServer,
        (true, false) => Lang.LauncherConfigSettingDialog_AuthLogin,
        (false, false) => "",
    };

    [JsonIgnore]
    public bool IsAddButton { get; set => SetProperty(ref field, value); }



    public LauncherConfigModel() { }


    public LauncherConfigModel(string id, int channel, int subChannel, ServerRegion region, string name, string icon, string smallIcon, bool canLogin, bool isBeta)
    {
        Id = id;
        Channel = channel;
        SubChannel = subChannel;
        Region = region;
        Name = name;
        SmallIcon = smallIcon;
        Icon = icon;
        CanLogin = canLogin;
        IsBeta = isBeta;
    }




    private static List<LauncherConfigModel> All => _all ?? GetAll();
    private static List<LauncherConfigModel>? _all;


    public LauncherConfig ToLauncherConfig()
    {
        return new LauncherConfig(Id, Channel, SubChannel, Region, IsBeta);
    }



    public static LauncherConfigModel? FromId(string? launcherId)
    {
        return All.FirstOrDefault(x => x.Id == launcherId);
    }


    public static List<LauncherConfigModel> GetAll()
    {
        _all = [.. GetDefault(), BiliGI, BiliHSR, BiliZZZ, .. LoadExtra()];
        return _all;
    }



    public static List<LauncherConfigModel> GetDefault()
    {
        if (CultureInfo.CurrentUICulture.Name is "zh-CN")
        {
            return [miHoYo, HoYoverse];
        }
        else
        {
            return [HoYoverse, miHoYo];
        }
    }



    public static List<LauncherConfigModel> GetEnabled()
    {
        List<LauncherConfigModel> list = new();
        try
        {
            string? enabled = AppConfig.EnabledLauncherIds;
            var all = GetAll();
            if (!string.IsNullOrWhiteSpace(enabled))
            {
                string[] splits = enabled.Split(',');
                foreach (var item in splits)
                {
                    if (all.FirstOrDefault(x => x.Id == item) is LauncherConfigModel model)
                    {
                        list.Add(model);
                    }
                }
            }
        }
        catch { }
        if (list.Count > 0)
        {
            return list;
        }
        else
        {
            return GetDefault();
        }
    }


    public static List<LauncherConfigModel> GetEditable()
    {
        List<LauncherConfigModel> all = GetAll();
        string? enabled = AppConfig.EnabledLauncherIds;
        if (!string.IsNullOrWhiteSpace(enabled))
        {
            foreach (var item in all)
            {
                if (enabled.Contains(item.Id))
                {
                    item.IsSelected = true;
                }
            }
        }
        if (all.Count(x => x.IsSelected) is 0)
        {
            all[0].IsSelected = true;
            all[1].IsSelected = true;
        }
        return all;
    }


    private static List<LauncherConfigModel> LoadExtra()
    {
        List<LauncherConfigModel>? list = null;
        try
        {
            string? json = AppConfig.ExtraLauncherConfigModel;
            if (!string.IsNullOrWhiteSpace(json))
            {
                list = JsonSerializer.Deserialize<List<LauncherConfigModel>>(json);
                if (list is not null)
                {
                    foreach (var item in list)
                    {
                        item.Editable = true;
                        if (!string.IsNullOrWhiteSpace(item.Icon))
                        {
                            item.Icon = Path.Join(AppConfig.UserDataFolder, "image", Path.GetFileName(item.Icon));
                        }
                        if (!string.IsNullOrWhiteSpace(item.SmallIcon))
                        {
                            item.SmallIcon = Path.Join(AppConfig.UserDataFolder, "image", Path.GetFileName(item.SmallIcon));
                        }
                    }
                }
            }
        }
        catch { }
        return list ?? [];
    }


    public static void SaveExtra(IEnumerable<LauncherConfigModel> models)
    {
        try
        {
            string json = JsonSerializer.Serialize(models);
            AppConfig.ExtraLauncherConfigModel = json;
        }
        catch { }
    }



    public static LauncherConfigModel miHoYo => new("jGHBHlcOq1", 1, 1, ServerRegion.miHoYo, "miHoYo", "ms-appx:///Assets/Image/icon_hyperion.png", "ms-appx:///Assets/Image/gameicon_hyperion.png", true, false);

    public static LauncherConfigModel HoYoverse => new("VYTpXlbWo8", 1, 0, ServerRegion.HoYoverse, "HoYoverse", "ms-appx:///Assets/Image/icon_hoyolab.png", "ms-appx:///Assets/Image/gameicon_hoyolab.png", true, false);

    public static LauncherConfigModel BiliGI => new("umfgRO5gh5", 14, 0, ServerRegion.miHoYo, "bilibili - 原神", "ms-appx:///Assets/Image/icon_bilibili.png", "ms-appx:///Assets/Image/gameicon_bilibili.png", false, false);

    public static LauncherConfigModel BiliHSR => new("6P5gHMNyK3", 14, 0, ServerRegion.miHoYo, "bilibili - 星穹铁道", "ms-appx:///Assets/Image/icon_bilibili.png", "ms-appx:///Assets/Image/gameicon_bilibili.png", false, false);

    public static LauncherConfigModel BiliZZZ => new("xV0f4r1GT0", 14, 0, ServerRegion.miHoYo, "bilibili - 绝区零", "ms-appx:///Assets/Image/icon_bilibili.png", "ms-appx:///Assets/Image/gameicon_bilibili.png", false, false);










}