using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Starward.Codec.JpegXL;

/// <summary>
/// Data type holding the 4-character type name of an ISOBMFF box.
/// </summary>
[InlineArray(4)]
public struct JxlBoxType : IEquatable<JxlBoxType>, IEquatable<ReadOnlySpan<byte>>, IEquatable<string>
{

    private byte _element0;

    /// <summary>
    /// Initializes a new instance of the <see cref="JxlBoxType"/> struct from a span of bytes.
    /// </summary>
    /// <param name="value">The span of bytes representing the box type. Must be 4 bytes or less.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is more than 4 bytes.</exception>
    public JxlBoxType(ReadOnlySpan<byte> value)
    {
        if (value.Length > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Box type byte span must be at most 4 bytes.");
        }
        value.CopyTo(MemoryMarshal.CreateSpan(ref _element0, 4));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JxlBoxType"/> struct from a string.
    /// </summary>
    /// <param name="value">The string representing the box type. Must be 4 characters or less.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is more than 4 characters.</exception>
    public JxlBoxType(string value)
    {
        if (value.Length > 4)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Box type string must be at most 4 characters.");
        }
        Encoding.UTF8.GetBytes(value, MemoryMarshal.CreateSpan(ref _element0, 4));
    }

    /// <summary>
    /// Indicates whether the current object is equal to another object of the same type.
    /// </summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns><see langword="true"/> if the current object is equal to the other parameter; otherwise, <see langword="false"/>.</returns>
    public bool Equals(JxlBoxType other)
    {
        return this.AsSpan().SequenceEqual(other.AsSpan());
    }

    /// <summary>
    /// Gets a read-only span representing the box type.
    /// </summary>
    /// <returns>A read-only span of bytes.</returns>
    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _element0, 4);
    }

    /// <summary>
    /// Indicates whether the current object is equal to a span of bytes.
    /// </summary>
    /// <param name="other">A span of bytes to compare with this object.</param>
    /// <returns><see langword="true"/> if the current object is equal to the other parameter; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ReadOnlySpan<byte> other)
    {
        return this.AsSpan().SequenceEqual(other);
    }

    /// <summary>
    /// Indicates whether the current object is equal to a string.
    /// </summary>
    /// <param name="other">A string to compare with this object.</param>
    /// <returns><see langword="true"/> if the current object is equal to the other parameter; otherwise, <see langword="false"/>.</returns>
    public bool Equals(string? other)
    {
        if (string.IsNullOrWhiteSpace(other) || other.Length > 4)
        {
            return false;
        }
        Span<byte> otherSpan = stackalloc byte[4];
        Encoding.UTF8.GetBytes(other, otherSpan);
        return this.AsSpan().SequenceEqual(otherSpan);
    }


    /// <summary>
    /// EXIF metadata box type ("Exif").
    /// </summary>
    public static JxlBoxType Exif => new("Exif"u8);

    /// <summary>
    /// XMP metadata box type ("xml ").
    /// </summary>
    public static JxlBoxType XMP => new("xml "u8);

    /// <summary>
    /// JUMBF metadata box type ("jumb ").
    /// </summary>
    public static JxlBoxType JUMBF => new("jumb "u8);

    /// <summary>
    /// HDR-GM metadata box type ("jhgm ").
    /// </summary>
    public static JxlBoxType HDRGainMap => new("jhgm "u8);


}