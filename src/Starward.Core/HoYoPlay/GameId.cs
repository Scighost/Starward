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
    public string Biz { get; set; }


    public GameBiz ToGameBiz()
    {
        GameBiz result = Id switch
        {
            "g0mMIvshDb" => GameBiz.bh3_jp,
            "uxB4MC7nzC" => GameBiz.bh3_kr,
            "bxPTXSET5t" => GameBiz.bh3_overseas,
            "wkE5P5WsIf" => GameBiz.bh3_tw,
            "T2S0Gz4Dr2" => GameBiz.hk4e_bilibili,
            "EdtUqXfCHh" => GameBiz.hkrpg_bilibili,
            "HXAFlmYa17" => GameBiz.nap_bilibili,
            _ => GameBiz.None,
        };
        if (result is GameBiz.None)
        {
            if (Enum.TryParse<GameBiz>(Biz, out var biz) && biz.ToGame() is not GameBiz.None)
            {
                result = biz;
            }
        }
        return result;
    }


    public static GameId? FromGameBiz(GameBiz gameBiz)
    {
        return gameBiz switch
        {
            GameBiz.bh3_cn => new GameId { Id = "osvnlOc0S8", Biz = "bh3_cn" },
            GameBiz.bh3_global => new GameId { Id = "5TIVvvcwtM", Biz = "bh3_global" },
            GameBiz.bh3_jp => new GameId { Id = "g0mMIvshDb", Biz = "bh3_jp" },
            GameBiz.bh3_kr => new GameId { Id = "uxB4MC7nzC", Biz = "bh3_kr" },
            GameBiz.bh3_overseas => new GameId { Id = "bxPTXSET5t", Biz = "bh3_overseas" },
            GameBiz.bh3_tw => new GameId { Id = "wkE5P5WsIf", Biz = "bh3_tw" },
            GameBiz.hk4e_cn => new GameId { Id = "1Z8W5NHUQb", Biz = "hk4e_cn" },
            GameBiz.hk4e_global => new GameId { Id = "gopR6Cufr3", Biz = "hk4e_global" },
            GameBiz.hk4e_bilibili => new GameId { Id = "T2S0Gz4Dr2", Biz = "hk4e_bilibili" },
            GameBiz.hkrpg_cn => new GameId { Id = "64kMb5iAWu", Biz = "hkrpg_cn" },
            GameBiz.hkrpg_global => new GameId { Id = "4ziysqXOQ8", Biz = "hkrpg_global" },
            GameBiz.hkrpg_bilibili => new GameId { Id = "EdtUqXfCHh", Biz = "hkrpg_bilibili" },
            GameBiz.nap_cn => new GameId { Id = "x6znKlJ0xK", Biz = "nap_cn" },
            GameBiz.nap_global => new GameId { Id = "U5hbdsT9W7", Biz = "nap_global" },
            GameBiz.nap_bilibili => new GameId { Id = "HXAFlmYa17", Biz = "nap_bilibili" },
            _ => null,
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
