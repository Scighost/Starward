using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// 游戏 ID
/// </summary>
public class GameId : IEquatable<GameId>
{

    [JsonPropertyName("id")]
    public string Id { get; set; }


    [JsonPropertyName("biz")]
    [JsonConverter(typeof(GameBizJsonConverter))]
    public GameBiz GameBiz { get; set; }



    public GameId() { }


    public GameId(string id, GameBiz gameBiz)
    {
        Id = id;
        GameBiz = gameBiz;
    }



    public static GameId? FromGameBiz(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.bh3_cn => new GameId { Id = "osvnlOc0S8", GameBiz = "bh3_cn" },
            GameBiz.bh3_global => new GameId { Id = "5TIVvvcwtM", GameBiz = "bh3_global" },
            //GameBiz.bh3_jp => new GameId { Id = "g0mMIvshDb", GameBiz = "bh3_global" },
            //GameBiz.bh3_kr => new GameId { Id = "uxB4MC7nzC", GameBiz = "bh3_global" },
            //GameBiz.bh3_os => new GameId { Id = "bxPTXSET5t", GameBiz = "bh3_global" },
            //GameBiz.bh3_asia => new GameId { Id = "wkE5P5WsIf", GameBiz = "bh3_global" },
            GameBiz.hk4e_cn => new GameId { Id = "1Z8W5NHUQb", GameBiz = "hk4e_cn" },
            GameBiz.hk4e_global => new GameId { Id = "gopR6Cufr3", GameBiz = "hk4e_global" },
            GameBiz.hk4e_bilibili => new GameId { Id = "T2S0Gz4Dr2", GameBiz = "hk4e_bilibili" },
            GameBiz.hkrpg_cn => new GameId { Id = "64kMb5iAWu", GameBiz = "hkrpg_cn" },
            GameBiz.hkrpg_global => new GameId { Id = "4ziysqXOQ8", GameBiz = "hkrpg_global" },
            GameBiz.hkrpg_bilibili => new GameId { Id = "EdtUqXfCHh", GameBiz = "hkrpg_bilibili" },
            GameBiz.nap_cn => new GameId { Id = "x6znKlJ0xK", GameBiz = "nap_cn" },
            GameBiz.nap_global => new GameId { Id = "U5hbdsT9W7", GameBiz = "nap_global" },
            GameBiz.nap_bilibili => new GameId { Id = "HXAFlmYa17", GameBiz = "nap_bilibili" },
            _ => null,
        };
    }


    public override string ToString()
    {
        return $"{GameBiz}-{Id}";
    }


    public bool Equals(GameId? other)
    {
        return this.Id == other?.Id;
    }


    public override bool Equals(object? obj)
    {
        return this.Equals(obj as GameId);
    }


    public override int GetHashCode()
    {
        return this.Id.GetHashCode();
    }


    public static bool operator ==(GameId? left, GameId? right)
    {
        return left?.Id == right?.Id;
    }


    public static bool operator !=(GameId? left, GameId? right) => !(left == right);



    public const string bh3_cn = "osvnlOc0S8";
    public const string bh3_glb = "5TIVvvcwtM";
    public const string bh3_jp = "g0mMIvshDb";
    public const string bh3_kr = "uxB4MC7nzC";
    public const string bh3_os = "bxPTXSET5t";
    public const string bh3_asia = "wkE5P5WsIf";

    public const string hk4e_cn = "1Z8W5NHUQb";
    public const string hk4e_global = "gopR6Cufr3";
    public const string hk4e_bili = "T2S0Gz4Dr2";

    public const string hkrpg_cn = "64kMb5iAWu";
    public const string hkrpg_global = "4ziysqXOQ8";
    public const string hkrpg_bili = "EdtUqXfCHh";

    public const string nap_cn = "x6znKlJ0xK";
    public const string nap_global = "U5hbdsT9W7";
    public const string nap_bili = "HXAFlmYa17";

    public const string hyg_cn = "ERYDdvf0w8";
    public const string hyg_global = "viiywh5G2H";

    public const string abc_cn = "m8TGERqknS";
    public const string abc_global = "yneO4yGc4x";










}
