using Starward.Core.Gacha;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Starward.Models;

internal class GachaLogExportFile
{


    public GachaLogExportFile(int uid, List<GachaLogItem> list)
    {
        var time = DateTimeOffset.Now;
        Info = new GachaLogExportInfo
        {
            Uid = uid.ToString(),
            ExportTime = time.ToString("yyyy-MM-dd HH:mm:ss"),
            ExportTimestamp = time.ToUnixTimeSeconds().ToString(),
            Count = list.Count.ToString(),
        };
        List = list;
    }



    public class GachaLogExportInfo
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; }

        [JsonPropertyName("export_time")]
        public string ExportTime { get; set; }

        [JsonPropertyName("export_timestamp")]
        public string ExportTimestamp { get; set; }

        [JsonPropertyName("export_app")]
        public string ExportApp { get; set; } = "Starward";

        [JsonPropertyName("export_app_version")]
        public string ExportAppVersion { get; set; } = AppConfig.AppVersion ?? "";

        [JsonPropertyName("count")]
        public string Count { get; set; }
    }


    [JsonPropertyName("info")]
    public GachaLogExportInfo Info { get; set; }


    [JsonPropertyName("list")]
    public List<GachaLogItem> List { get; set; }


}
