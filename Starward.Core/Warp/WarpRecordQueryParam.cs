namespace Starward.Core.Warp;

internal struct WarpRecordQueryParam
{
    public WarpRecordQueryParam(WarpType warpType, int page, int size, long endId)
    {
        WarpType = warpType;
        Page = page;
        Size = size;
        EndId = endId;
    }

    public WarpType WarpType { get; set; }

    public int Page { get; set; }

    public int Size { get; set; }

    public long EndId { get; set; }


    public override string ToString()
    {
        return $"page={Page}&size={Size}&gacha_type={(int)WarpType}&end_id={EndId}";
    }

}
