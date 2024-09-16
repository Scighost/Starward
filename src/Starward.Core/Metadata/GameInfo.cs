using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.Metadata;

public class GameInfo
{


    public string Name { get; set; }


    [JsonConverter(typeof(GameBizJsonConverter))]
    public GameBiz GameBiz { get; set; }


    public string Slogan { get; set; }


    public string Description { get; set; }


    public string HomePage { get; set; }


    public string Logo { get; set; }


    public string Poster { get; set; }


    public string HoYoSlogan { get; set; }


    public List<string> Fonts { get; set; }


}

