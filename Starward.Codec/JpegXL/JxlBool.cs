using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL;

/// <summary>
/// A portable bool replacement.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct JxlBool : IEquatable<JxlBool>
{
    /// <summary>
    /// True value
    /// </summary>
    public static readonly JxlBool True = true;

    /// <summary>
    /// False value
    /// </summary>
    public static readonly JxlBool False = false;

    /// <summary>
    /// The underlying integer value.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="JxlBool"/> struct.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public JxlBool(bool value)
    {
        Value = value ? 1 : 0;
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><see langword="true"/> if the current object is equal to the other parameter; otherwise, <see langword="false"/>.</returns>
    public bool Equals(JxlBool other)
    {
        return (bool)this == (bool)other;
    }

    /// <summary>
    /// Implicit conversion from <see cref="bool"/> to <see cref="JxlBool"/>.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    public static implicit operator JxlBool(bool value)
    {
        return new(value);
    }

    /// <summary>
    /// Implicit conversion from <see cref="JxlBool"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="value">The JxlBool value.</param>
    public static implicit operator bool(JxlBool value)
    {
        return value.Value != 0;
    }
}