using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Starward.Codec;

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


[InlineArray(4)]
public struct FixedArray4<T> where T : struct
{
    private T _value0;

    public FixedArray4(T value)
    {
        var span = MemoryMarshal.CreateSpan(ref _value0, 4);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = value;
        }
    }


    public FixedArray4(T value0, T value1, T value2, T value3)
    {
        var span = MemoryMarshal.CreateSpan(ref _value0, 3);
        span[0] = value0;
        span[1] = value1;
        span[2] = value2;
        span[3] = value3;
    }

}


[InlineArray(16)]
public struct FixedArray16<T> where T : struct
{
    private T _value0;

    public FixedArray16(T value)
    {
        var span = MemoryMarshal.CreateSpan(ref _value0, 16);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = value;
        }
    }

}