using Starward.Core;

namespace Starward.Features.Gacha;

internal class UpdateGachaLogMessage
{

    public GameBiz GameBiz { get; set; }


    public string Url { get; set; }



    public UpdateGachaLogMessage(GameBiz gameBiz, string url)
    {
        GameBiz = gameBiz;
        Url = url;
    }

}
