using System;

namespace Starward.Models;

public class GachaLogUrl
{

    public GachaLogUrl() { }


    public GachaLogUrl(int uid, string gachaUrl)
    {
        Uid = uid;
        GachaUrl = gachaUrl;
        Time = DateTime.Now;
    }


    public int Uid { get; set; }


    public string GachaUrl { get; set; }


    public DateTime Time { get; set; }


}
