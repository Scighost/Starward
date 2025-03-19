using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core;
using System;

namespace Starward.Features.GameAccount;

public class GameAccount : ObservableObject
{

    public string SHA256 { get; set; }

    public GameBiz GameBiz { get; set; }

    public long Uid { get; set => SetProperty(ref field, value); }

    public string Name { get; set => SetProperty(ref field, value); }

    public byte[] Value { get; set; }

    public DateTime Time { get; set; } = DateTime.Now;

    public bool IsSaved { get; set => SetProperty(ref field, value); }

}