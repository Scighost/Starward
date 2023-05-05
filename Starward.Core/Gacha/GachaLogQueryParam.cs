namespace Starward.Core.Gacha;

internal struct GachaLogQueryParam
{
    public GachaLogQueryParam(GachaType gachaType, int page, int size, long endId)
    {
        GachaType = gachaType;
        Page = page;
        Size = size;
        EndId = endId;
    }

    public GachaType GachaType { get; set; }

    public int Page { get; set; }

    public int Size { get; set; }

    public long EndId { get; set; }


    public override string ToString()
    {
        return $"page={Page}&size={Size}&gacha_type={(int)GachaType}&end_id={EndId}";
    }

}
