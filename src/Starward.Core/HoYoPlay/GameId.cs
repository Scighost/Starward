using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// 游戏 ID
/// </summary>
public class GameId
{

    [JsonPropertyName("id")]
    public string Id { get; set; }


    [JsonPropertyName("biz")]
    public string Biz { get; set; }


    public GameBiz ToGameBiz()
    {
        if (Enum.TryParse<GameBiz>(Biz, out var biz) && biz.ToGame() is not GameBiz.None)
        {
            return biz;
        }
        else
        {
            return GameBiz.None;
        }
    }


    public static GameId? FromGameBiz(GameBiz gameBiz)
    {
        return gameBiz switch
        {
            GameBiz.bh3_cn => new GameId { Id = "osvnlOc0S8", Biz = "bh3_cn" },
            GameBiz.bh3_global => new GameId { Id = "5TIVvvcwtM", Biz = "bh3_global" },
            GameBiz.hk4e_cn => new GameId { Id = "1Z8W5NHUQb", Biz = "hk4e_cn" },
            GameBiz.hk4e_global => new GameId { Id = "gopR6Cufr3", Biz = "hk4e_global" },
            GameBiz.hk4e_bilibili => new GameId { Id = "T2S0Gz4Dr2", Biz = "hk4e_cn" },
            GameBiz.hkrpg_cn => new GameId { Id = "64kMb5iAWu", Biz = "hkrpg_cn" },
            GameBiz.hkrpg_global => new GameId { Id = "4ziysqXOQ8", Biz = "hkrpg_global" },
            GameBiz.hkrpg_bilibili => new GameId { Id = "EdtUqXfCHh", Biz = "hkrpg_cn" },
            GameBiz.nap_cn => new GameId { Id = "x6znKlJ0xK", Biz = "nap_cn" },
            GameBiz.nap_global => new GameId { Id = "U5hbdsT9W7", Biz = "nap_global" },
            GameBiz.nap_bilibili => new GameId { Id = "", Biz = "nap_bilibili" },
            _ => null,
        };
    }


}
