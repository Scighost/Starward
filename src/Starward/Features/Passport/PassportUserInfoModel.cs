using CommunityToolkit.Mvvm.ComponentModel;
using Starward.Core.Passport;

namespace Starward.Features.Passport;

[ObservableObject]
public partial class PassportUserInfoModel : PassportUserInfo
{
    public string Nickname { get; set => SetProperty(ref field, value); }

    public string AvatarUrl { get; set => SetProperty(ref field, value); }

    public string? Pendant { get; set => SetProperty(ref field, value); }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowAlert))]
    public partial PassportErrorType ErrorType { get; set; }

    public bool ShowAlert => ErrorType is not PassportErrorType.Success;
}


public enum PassportErrorType
{
    Success = 0,

    NeedLogin = 1,

    NeedPassword = 2,
}