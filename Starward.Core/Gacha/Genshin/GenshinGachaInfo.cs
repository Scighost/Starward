using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.Genshin;

public class GenshinGachaInfo : IJsonOnSerializing, IJsonOnDeserialized
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("head_icon")]
    public string HeadIcon { get; set; }

    [JsonPropertyName("element")]
    public int Element { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// for weapon
    /// </summary>
    [JsonPropertyName("cat_id")]
    public int CatId { get; set; }

    /// <summary>
    /// for avatar
    /// </summary>
    [JsonPropertyName("weapon_cat_id")]
    public int WeaponCatId { get; set; }


    public void OnDeserialized()
    {
        if (!string.IsNullOrWhiteSpace(HeadIcon))
        {
            (Icon, HeadIcon) = (HeadIcon, Icon);
        }
    }


    public void OnSerializing()
    {
        if (!string.IsNullOrWhiteSpace(HeadIcon))
        {
            (Icon, HeadIcon) = (HeadIcon, Icon);
        }
    }

}
