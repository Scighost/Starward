using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace Starward.Features.Gacha.ZZZGachaToolbox;

/// <summary>
/// https://act-api-takumi.mihoyo.com/event/nap_cultivate_tool/user/item_list
/// https://sg-public-api.hoyolab.com/event/nap_cultivate_tool/user/item_list
/// </summary>
internal class ItemList
{

    [JsonPropertyName("weapon")]
    public List<ItemListWeapon> Weapons { get; set; }

    [JsonPropertyName("avatar_list")]
    public List<ItemListAvatar> Avatars { get; set; }

    [JsonPropertyName("buddy_list")]
    public List<ItemListBuddy> Buddies { get; set; }

}

internal class ItemListWeapon
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

    [JsonPropertyName("profession")]
    public int Profession { get; set; }
}



internal class ItemListAvatar
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string NameMi18n { get; set; }

    [JsonPropertyName("full_name_mi18n")]
    public string FullNameMi18n { get; set; }

    [JsonPropertyName("element_type")]
    public int ElementType { get; set; }

    [JsonPropertyName("camp_name_mi18n")]
    public string CampNameMi18n { get; set; }

    [JsonPropertyName("avatar_profession")]
    public int AvatarProfession { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

    [JsonPropertyName("group_icon_path")]
    public string GroupIconPath { get; set; }

    [JsonPropertyName("hollow_icon_path")]
    public string HollowIconPath { get; set; }

}


internal class ItemListBuddy
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }
}

