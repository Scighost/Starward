namespace Starward.Core.Passport;

public class PassportUserInfo
{

    public ServerRegion Region { get; set; }

    public long Aid { get; set; }

    public string Mid { get; set; }

    public string Stoken { get; set; }


    public bool IsChinaServer => Region is ServerRegion.miHoYo;

    public bool IsGlobalServer => Region is ServerRegion.HoYoverse;

}
