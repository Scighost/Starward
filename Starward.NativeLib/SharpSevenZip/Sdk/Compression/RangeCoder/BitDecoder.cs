namespace SharpSevenZip.Sdk.Compression.RangeCoder;

internal struct BitDecoder
{
    public const uint kBitModelTotal = (1 << kNumBitModelTotalBits);
    public const int kNumBitModelTotalBits = 11;
    private const int kNumMoveBits = 5;

    private uint Prob;

    /*public void UpdateModel(int numMoveBits, uint symbol)
    {
        if (symbol == 0)
            Prob += (kBitModelTotal - Prob) >> numMoveBits;
        else
            Prob -= (Prob) >> numMoveBits;
    }*/

    public void Init()
    {
        Prob = kBitModelTotal >> 1;
    }

    public uint Decode(Decoder rangeDecoder)
    {
        uint newBound = (rangeDecoder.Range >> kNumBitModelTotalBits) * Prob;
        if (rangeDecoder.Code < newBound)
        {
            rangeDecoder.Range = newBound;
            Prob += (kBitModelTotal - Prob) >> kNumMoveBits;
            if (rangeDecoder.Range < Decoder.kTopValue)
            {
                rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream!.ReadByte();
                rangeDecoder.Range <<= 8;
            }
            return 0;
        }
        else
        {
            rangeDecoder.Range -= newBound;
            rangeDecoder.Code -= newBound;
            Prob -= (Prob) >> kNumMoveBits;
            if (rangeDecoder.Range < Decoder.kTopValue)
            {
                rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream!.ReadByte();
                rangeDecoder.Range <<= 8;
            }
            return 1;
        }
    }
}
