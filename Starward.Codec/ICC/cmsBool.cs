using System.Runtime.InteropServices;

namespace Starward.Codec.ICC;


[StructLayout(LayoutKind.Sequential)]
internal readonly struct cmsBool : IEquatable<cmsBool>
{

    public static readonly cmsBool True = true;


    public static readonly cmsBool False = false;


    public readonly int Value;


    public cmsBool(bool value)
    {
        Value = value ? 1 : 0;
    }

    public bool Equals(cmsBool other)
    {
        return (bool)this == (bool)other;
    }

    public static implicit operator cmsBool(bool value)
    {
        return new(value);
    }

    public static implicit operator bool(cmsBool value)
    {
        return value.Value != 0;
    }
}
