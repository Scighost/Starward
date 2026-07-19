using Starward.Core;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Features.Gacha;




public class GachaNoUp
{

    public GameBiz Game { get; set; }

    public int GachaType { get; set; }

    public Dictionary<int, GachaNoUpItem> Items { get; set; } = new();



    public static Dictionary<string, GachaNoUp> Dictionary { get; } = new();



    static GachaNoUp()
    {
        AddGachaNoUpGenshin();
        AddGachaNoUpStarRail();
        AddGachaNoUpZZZ();
    }



    private static void AddGachaNoUpGenshin()
    {
        GachaNoUp hk4e301 = new GachaNoUp
        {
            Game = GameBiz.hk4e,
            GachaType = GenshinGachaType.CharacterEventWish,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 10000003,
                    Name = "琴",
                    NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 10000016,
                    Name = "迪卢克",
                    NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 10000035,
                    Name = "七七",
                    NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 10000041,
                    Name = "莫娜",
                    NoUpTimes = [(new DateTime(2020, 9, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 10000042,
                    Name = "刻晴",
                    NoUpTimes =
                    [
                        (new DateTime(2020, 9, 1), new DateTime(2021, 2, 17, 17, 59, 59)),
                        (new DateTime(2021, 3, 2, 16, 00, 00), DateTime.MaxValue),
                    ],
                },
                new GachaNoUpItem
                {
                    Id = 10000069,
                    Name = "提纳里",
                    NoUpTimes = [(new DateTime(2022, 9, 27, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 10000079,
                    Name = "迪希雅",
                    NoUpTimes = [(new DateTime(2023, 4, 11, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 10000109,
                    Name = "梦见月瑞希",
                    NoUpTimes = [(new DateTime(2025, 3, 25, 18, 00, 00), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("hk4e301", hk4e301);
    }


    private static void AddGachaNoUpStarRail()
    {
        GachaNoUp hkrpg11 = new GachaNoUp
        {
            Game = GameBiz.hkrpg,
            GachaType = StarRailGachaType.CharacterEventWarp,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 1003,
                    Name = "姬子",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1004,
                    Name = "瓦尔特",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1101,
                    Name = "布洛妮娅",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1104,
                    Name = "杰帕德",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1107,
                    Name = "克拉拉",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1209,
                    Name = "彦卿",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1211,
                    Name = "白露",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                // 3.2版本，自定义非UP五星角色
                new GachaNoUpItem
                {
                    Id = 1102,
                    Name = "希儿",
                    NoUpTimes = [(new DateTime(2025, 4, 8, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1205,
                    Name = "刃",
                    NoUpTimes =
                    [
                        (new DateTime(2025, 4, 8, 18, 00, 00), new DateTime(2025, 7, 23, 11, 59, 59)),
                        (new DateTime(2025, 8, 12, 15, 00, 00), DateTime.MaxValue),
                    ],
                },
                new GachaNoUpItem
                {
                    Id = 1208,
                    Name = "符玄",
                    NoUpTimes = [(new DateTime(2025, 4, 8, 18, 00, 00), DateTime.MaxValue)],
                },
                // 4.2版本，自定义非UP五星角色
                new GachaNoUpItem
                {
                    Id = 1006,
                    Name = "银狼",
                    NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1221,
                    Name = "云璃",
                    NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1302,
                    Name = "银枝",
                    NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("hkrpg11", hkrpg11);

        GachaNoUp hkrpg12 = new GachaNoUp
        {
            Game = GameBiz.hkrpg,
            GachaType = StarRailGachaType.LightConeEventWarp,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 23000,
                    Name = "银河铁道之夜",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23002,
                    Name = "无可取代的东西",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23003,
                    Name = "但战斗还未结束",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23004,
                    Name = "以世界之名",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23005,
                    Name = "制胜的瞬间",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23012,
                    Name = "如泥酣眠",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23013,
                    Name = "时节不居",
                    NoUpTimes = [(new DateTime(2023, 4, 1), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("hkrpg12", hkrpg12);

        GachaNoUp hkrpg21 = new GachaNoUp
        {
            Game = GameBiz.hkrpg,
            GachaType = StarRailGachaType.CharacterCollaborationWarp,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 1003,
                    Name = "姬子",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1004,
                    Name = "瓦尔特",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1101,
                    Name = "布洛妮娅",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1104,
                    Name = "杰帕德",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1107,
                    Name = "克拉拉",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1209,
                    Name = "彦卿",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1211,
                    Name = "白露",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                // 3.2版本，自定义非UP五星角色
                new GachaNoUpItem
                {
                    Id = 1102,
                    Name = "希儿",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1205,
                    Name = "刃",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1208,
                    Name = "符玄",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                // 4.2版本，自定义非UP五星角色
                new GachaNoUpItem
                {
                    Id = 1006,
                    Name = "银狼",
                    NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1221,
                    Name = "云璃",
                    NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1302,
                    Name = "银枝",
                    NoUpTimes = [(new DateTime(2026, 4, 21, 18, 00, 00), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("hkrpg21", hkrpg21);

        GachaNoUp hkrpg22 = new GachaNoUp
        {
            Game = GameBiz.hkrpg,
            GachaType = StarRailGachaType.LightConeCollaborationWarp,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 23000,
                    Name = "银河铁道之夜",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23002,
                    Name = "无可取代的东西",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23003,
                    Name = "但战斗还未结束",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23004,
                    Name = "以世界之名",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23005,
                    Name = "制胜的瞬间",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23012,
                    Name = "如泥酣眠",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 23013,
                    Name = "时节不居",
                    NoUpTimes = [(new DateTime(2025, 7, 11), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("hkrpg22", hkrpg22);
    }


    private static void AddGachaNoUpZZZ()
    {
        GachaNoUp nap2 = new GachaNoUp
        {
            Game = GameBiz.nap,
            GachaType = ZZZGachaType.ExclusiveChannel,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 1021,
                    Name = "猫又",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1041,
                    Name = "「11号」",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1101,
                    Name = "珂蕾妲",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1141,
                    Name = "莱卡恩",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1181,
                    Name = "格莉丝",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1211,
                    Name = "丽娜",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1071,
                    Name = "凯撒",
                    NoUpTimes = [(new DateTime(2026, 7, 28, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1221,
                    Name = "柳",
                    NoUpTimes = [(new DateTime(2026, 7, 28, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 1241,
                    Name = "朱鸢",
                    NoUpTimes = [(new DateTime(2026, 7, 28, 18, 00, 00), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("nap2", nap2);
        Dictionary.Add("nap102", nap2);

        GachaNoUp nap3 = new GachaNoUp
        {
            Game = GameBiz.nap,
            GachaType = ZZZGachaType.WEngineChannel,
            Items = new List<GachaNoUpItem>
            {
                new GachaNoUpItem
                {
                    Id = 14102,
                    Name = "钢铁肉垫",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14104,
                    Name = "硫磺石",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14110,
                    Name = "燃狱齿轮",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14114,
                    Name = "拘缚者",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14118,
                    Name = "嵌合编译器",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14121,
                    Name = "啜泣摇篮",
                    NoUpTimes = [(new DateTime(2024, 7, 1), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14107,
                    Name = "奔袭獠牙",
                    NoUpTimes = [(new DateTime(2026, 7, 28, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14122,
                    Name = "时流贤者",
                    NoUpTimes = [(new DateTime(2026, 7, 28, 18, 00, 00), DateTime.MaxValue)],
                },
                new GachaNoUpItem
                {
                    Id = 14124,
                    Name = "防暴者Ⅵ型",
                    NoUpTimes = [(new DateTime(2026, 7, 28, 18, 00, 00), DateTime.MaxValue)],
                },
            }.ToDictionary(item => item.Id),
        };
        Dictionary.Add("nap3", nap3);
        Dictionary.Add("nap103", nap3);
    }


}



public class GachaNoUpItem
{

    public int Id { get; set; }

    public string Name { get; set; }

    public List<(DateTime Start, DateTime End)> NoUpTimes { get; set; }

}
