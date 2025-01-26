using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.UpgradeGuide;

public class UpgradeGuideItemList
{


    [JsonPropertyName("equip_suit")]
    public List<UpgradeGuideEquipSuit> EquipSuit { get; set; }


    [JsonPropertyName("weapon")]
    public List<UpgradeGuideWeapon> Weapon { get; set; }


    [JsonPropertyName("avatar_list")]
    public List<UpgradeGuideAvatar> AvatarList { get; set; }


    [JsonPropertyName("buddy_list")]
    public List<UpgradeGuideBuddy> BuddyList { get; set; }


}


/// <summary>
/// 音擎
/// </summary>
public class UpgradeGuideWeapon
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


/// <summary>
/// 驱动盘套装
/// </summary>
public class UpgradeGuideEquipSuit
{

    [JsonPropertyName("suit_id")]
    public int SuitId { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("desc1")]
    public string Desc1 { get; set; }


    [JsonPropertyName("desc2")]
    public string Desc2 { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

}



/// <summary>
/// 角色
/// </summary>
public class UpgradeGuideAvatar
{

    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }


    [JsonPropertyName("name_mi18n")]
    public string Name { get; set; }


    [JsonPropertyName("full_name_mi18n")]
    public string FullName { get; set; }


    [JsonPropertyName("element_type")]
    public int ElementType { get; set; }


    [JsonPropertyName("camp_name_mi18n")]
    public string CampName { get; set; }


    [JsonPropertyName("avatar_profession")]
    public int AvatarProfession { get; set; }


    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }


    [JsonPropertyName("group_icon_path")]
    public string GroupIconPath { get; set; }


    [JsonPropertyName("hollow_icon_path")]
    public string HollowIconPath { get; set; }

}


/// <summary>
/// 邦布
/// </summary>
public class UpgradeGuideBuddy
{

    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

}
