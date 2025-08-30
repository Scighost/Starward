using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;

[InlineArray(3)]
public struct FixedArray3<T> where T : struct
{
    private T _value0;

    public FixedArray3(T value)
    {
        var span = MemoryMarshal.CreateSpan(ref _value0, 3);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = value;
        }
    }


    public FixedArray3(T value0, T value1, T value2)
    {
        var span = MemoryMarshal.CreateSpan(ref _value0, 3);
        span[0] = value0;
        span[1] = value1;
        span[2] = value2;
    }

}