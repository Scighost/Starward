namespace SharpSevenZip.Sdk.Compression.RangeCoder;

internal readonly struct BitTreeDecoder
{
    private readonly BitDecoder[] Models;
    private readonly int NumBitLevels;

    public BitTreeDecoder(int numBitLevels)
    {
        NumBitLevels = numBitLevels;
        Models = new BitDecoder[1 << numBitLevels];
    }

    public void Init()
    {
        for (uint i = 1; i < (1 << NumBitLevels); i++)
            Models[i].Init();
    }

    public uint Decode(Decoder rangeDecoder)
    {
        uint m = 1;
        for (int bitIndex = NumBitLevels; bitIndex > 0; bitIndex--)
            m = (m << 1) + Models[m].Decode(rangeDecoder);
        return m - ((uint)1 << NumBitLevels);
    }

    public uint ReverseDecode(Decoder rangeDecoder)
    {
        uint m = 1;
        uint symbol = 0;
        for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
        {
            uint bit = Models[m].Decode(rangeDecoder);
            m <<= 1;
            m += bit;
            symbol |= (bit << bitIndex);
        }
        return symbol;
    }

    public static uint ReverseDecode(BitDecoder[] Models, uint startIndex,
                                     Decoder rangeDecoder, int NumBitLevels)
    {
        uint m = 1;
        uint symbol = 0;
        for (int bitIndex = 0; bitIndex < NumBitLevels; bitIndex++)
        {
            uint bit = Models[startIndex + m].Decode(rangeDecoder);
            m <<= 1;
            m += bit;
            symbol |= (bit << bitIndex);
        }
        return symbol;
    }
}
