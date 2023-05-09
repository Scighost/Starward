namespace Starward.Core.Gacha;

public struct GachaLogQuery
{

    public int GachaType { get; set; }

    public int Page { get; set; }

    public long EndId { get; set; }

    public int Size { get; set; }



    public GachaLogQuery(int gachaType, int page, int size, long endId)
    {
        GachaType = gachaType;
        Page = page;
        Size = size;
        EndId = endId;
    }

    public override string ToString()
    {
        return $"gacha_type={GachaType}&page={Page}&size={Size}&end_id={EndId}";
    }
}


