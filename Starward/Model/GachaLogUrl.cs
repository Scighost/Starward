using Starward.Core;
using System;

namespace Starward.Model;

public class GachaLogUrl
{

    public GachaLogUrl() { }


    public GachaLogUrl(int uid, string url)
    {
        Uid = uid;
        Url = url;
        Time = DateTime.Now;
    }


    public GameBiz GameBiz { get; set; }


    public int Uid { get; set; }


    public string Url { get; set; }


    public DateTime Time { get; set; }


}
