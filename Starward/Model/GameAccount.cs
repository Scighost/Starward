using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Starward.Model;

public class GameAccount : ObservableObject
{

    public string SHA256 { get; set; }

    private int _Uid;
    public int Uid
    {
        get => _Uid;
        set => SetProperty(ref _Uid, value);
    }

    private string _Name;
    public string Name
    {
        get => _Name;
        set => SetProperty(ref _Name, value);
    }

    public int Server { get; set; }

    public byte[] Value { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;

    private bool _IsLogin;
    public bool IsLogin
    {
        get => _IsLogin;
        set => SetProperty(ref _IsLogin, value);
    }


}
