using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core.HoYoPlay;
using System.Collections.Generic;


namespace Starward.Features.GameSelector;


public class InstalledGameModel : ObservableObject
{
    public GameId GameId { get; set; }

    public string LauncherId { get; set; }

    public string GameIcon { get; set => SetProperty(ref field, value); }

    public string GameName { get; set => SetProperty(ref field, value); }

    public string LauncherIcon { get; set => SetProperty(ref field, value); }

    public string LauncherName { get; set => SetProperty(ref field, value); }

    public string SizeText { get; set => SetProperty(ref field, value); }

    public string InstallPath { get; set => SetProperty(ref field, value); }

    public bool Removable { get; set => SetProperty(ref field, value); }

    public bool IsPinned { get; set => SetProperty(ref field, value); }

    public string? SharedSizeText { get; set => SetProperty(ref field, value); }

    public List<InstalledGameHardLinkSharedModel>? SharedModels { get; set => SetProperty(ref field, value); }
}


public class InstalledGameHardLinkSharedModel : ObservableObject
{
    public string GameIcon { get; set => SetProperty(ref field, value); }

    public string GameName { get; set => SetProperty(ref field, value); }

    public string LauncherIcon { get; set => SetProperty(ref field, value); }

    public string LauncherName { get; set => SetProperty(ref field, value); }

    public string SizeText { get; set => SetProperty(ref field, value); }
}