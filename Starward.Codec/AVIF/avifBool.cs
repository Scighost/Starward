using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

/// <summary>
/// A portable bool replacement.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct avifBool : IEquatable<avifBool>
{
    /// <summary>
    /// True value
    /// </summary>
    public static readonly avifBool True = true;

    /// <summary>
    /// False value
    /// </summary>
    public static readonly avifBool False = false;

    /// <summary>
    /// The underlying integer value.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="avifBool"/> struct.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public avifBool(bool value)
    {
        Value = value ? 1 : 0;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><see langword="true"/> if the current object is equal to the other parameter; otherwise, <see langword="false"/>.</returns>
    public bool Equals(avifBool other)
    {
        return (bool)this == (bool)other;
    }

    /// <summary>
    /// Implicit conversion from <see cref="bool"/> to <see cref="avifBool"/>.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public static implicit operator avifBool(bool value)
    {
        return new(value);
    }

    /// <summary>
    /// Implicit conversion from <see cref="avifBool"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="value">The JxlBool value.</param>
    public static implicit operator bool(avifBool value)
    {
        return value.Value != 0;
    }


    public override string ToString()
    {
        return ((bool)this).ToString();
    }

}
