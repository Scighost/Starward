namespace SharpSevenZip.Sdk.Compression.RangeCoder;

internal readonly struct BitTreeEncoder
{
    private readonly BitEncoder[] Models;
    private readonly int NumBitLevels;

    public BitTreeEncoder(int numBitLevels)
    {
        NumBitLevels = numBitLevels;
        Models = new BitEncoder[1 << numBitLevels];
    }

    public void Init()
    {
        for (uint i = 1; i < (1 << NumBitLevels); i++)
            Models[i].Init();
    }

    public void Encode(Encoder rangeEncoder, uint symbol)
    {
        uint m = 1;
        for (int bitIndex = NumBitLevels; bitIndex > 0;)
        {
            bitIndex--;
            uint bit = (symbol >> bitIndex) & 1;
            Models[m].Encode(rangeEncoder, bit);
            m = (m << 1) | bit;
        }
    }

    public void ReverseEncode(Encoder rangeEncoder, uint symbol)
    {
        uint m = 1;
        for (uint i = 0; i < NumBitLevels; i++)
        {
            uint bit = symbol & 1;
            Models[m].Encode(rangeEncoder, bit);
            m = (m << 1) | bit;
            symbol >>= 1;
        }
    }

    public uint GetPrice(uint symbol)
    {
        uint price = 0;
        uint m = 1;
        for (int bitIndex = NumBitLevels; bitIndex > 0;)
        {
            bitIndex--;
            uint bit = (symbol >> bitIndex) & 1;
            price += Models[m].GetPrice(bit);
            m = (m << 1) + bit;
        }
        return price;
    }

    public uint ReverseGetPrice(uint symbol)
    {
        uint price = 0;
        uint m = 1;
        for (int i = NumBitLevels; i > 0; i--)
        {
            uint bit = symbol & 1;
            symbol >>= 1;
            price += Models[m].GetPrice(bit);
            m = (m << 1) | bit;
        }
        return price;
    }

    public static uint ReverseGetPrice(BitEncoder[] Models, uint startIndex,
                                         int NumBitLevels, uint symbol)
    {
        uint price = 0;
        uint m = 1;
        for (int i = NumBitLevels; i > 0; i--)
        {
            uint bit = symbol & 1;
            symbol >>= 1;
            price += Models[startIndex + m].GetPrice(bit);
            m = (m << 1) | bit;
        }
        return price;
    }

    public static void ReverseEncode(BitEncoder[] Models, uint startIndex,
                                     Encoder rangeEncoder, int NumBitLevels, uint symbol)
    {
        uint m = 1;
        for (int i = 0; i < NumBitLevels; i++)
        {
            uint bit = symbol & 1;
            Models[startIndex + m].Encode(rangeEncoder, bit);
            m = (m << 1) | bit;
            symbol >>= 1;
        }
    }
}
