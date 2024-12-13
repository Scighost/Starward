using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core.HoYoPlay;
using System.Collections.Generic;


namespace Starward.Features.GameSelector;

public class GameBizDisplay
{

    public GameInfo GameInfo { get; set; }


    public List<GameBizDisplayServer> Servers { get; set; } = new();

}


public class GameBizDisplayServer : ObservableObject
{

    public GameBizIcon GameBizIcon { get; set; }


    public bool IsPinned { get; set => SetProperty(ref field, value); }


}
