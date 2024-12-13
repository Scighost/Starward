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


    public static GameId? FromGameBiz(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.bh3_cn => new GameId { Id = "osvnlOc0S8", GameBiz = "bh3_cn" },
            GameBiz.bh3_global => new GameId { Id = "5TIVvvcwtM", GameBiz = "bh3_global" },
            GameBiz.bh3_jp => new GameId { Id = "g0mMIvshDb", GameBiz = "bh3_global" },
            GameBiz.bh3_kr => new GameId { Id = "uxB4MC7nzC", GameBiz = "bh3_global" },
            GameBiz.bh3_os => new GameId { Id = "bxPTXSET5t", GameBiz = "bh3_global" },
            GameBiz.bh3_asia => new GameId { Id = "wkE5P5WsIf", GameBiz = "bh3_global" },
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



    public bool IsBilibiliServer()
    {
        return Id switch
        {
            "T2S0Gz4Dr2" => true,
            "EdtUqXfCHh" => true,
            "HXAFlmYa17" => true,
            _ => false,
        };
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


}
