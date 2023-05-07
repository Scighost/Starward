using System;

namespace Starward.Models;

public class WarpRecordUrl
{

    public WarpRecordUrl() { }


    public WarpRecordUrl(int uid, string warpUrl)
    {
        Uid = uid;
        WarpUrl = warpUrl;
        Time = DateTime.Now;
    }


    public int Uid { get; set; }


    public string WarpUrl { get; set; }


    public DateTime Time { get; set; }


}
