using Starward.Core.Gacha.ZZZ;

namespace Starward.Core.Gacha;

public struct GachaLogQuery
{

    public IGachaType GachaType { get; set; }

    public int Page { get; set; }

    public long EndId { get; set; }

    public int Size { get; set; }



    public GachaLogQuery(IGachaType gachaType, int page, int size, long endId)
    {
        GachaType = gachaType;
        Page = page;
        Size = size;
        EndId = endId;
    }


    public override string ToString()
    {
        if (GachaType is ZZZGachaType)
        {
            return $"real_gacha_type={GachaType}&page={Page}&size={Size}&end_id={EndId}";
        }
        else
        {
            return $"gacha_type={GachaType}&page={Page}&size={Size}&end_id={EndId}";
        }
    }


}


