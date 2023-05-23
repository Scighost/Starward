using Starward.Core.Gacha;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Starward.Models;

internal class GachaLogExportFile<T> where T : GachaLogItem
{


    public GachaLogExportFile(int uid, List<T> list)
    {
        var time = DateTimeOffset.Now;
        Info = new WarpRecordExportInfo
        {
            Uid = uid.ToString(),
            ExportTime = time.ToString("yyyy-MM-dd HH:mm:ss"),
            ExportTimestamp = time.ToUnixTimeSeconds().ToString(),
            Count = list.Count.ToString(),
        };
        List = list;
    }



    public class WarpRecordExportInfo
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
    public WarpRecordExportInfo Info { get; set; }


    [JsonPropertyName("list")]
    public List<T> List { get; set; }


}
