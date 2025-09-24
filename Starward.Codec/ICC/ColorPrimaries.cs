using System.Numerics;

namespace Starward.Codec.ICC;

public class ColorPrimaries
{

    public int Id { get; init; }

    public Vector2 Red { get; set; }

    public Vector2 Green { get; set; }

    public Vector2 Blue { get; set; }

    public Vector2 White { get; set; }



    public bool IsValid => Red.X > 0 && Red.Y > 0 && Green.X > 0 && Green.Y > 0 && Blue.X > 0 && Blue.Y > 0 && White.X > 0 && White.Y > 0;


    public Matrix4x4 GetRGBToXYZMatrix()
    {
        Vector3 xyz_r = new(Red.X / Red.Y, 1, (1 - Red.X - Red.Y) / Red.Y);
        Vector3 xyz_g = new(Green.X / Green.Y, 1, (1 - Green.X - Green.Y) / Green.Y);
        Vector3 xyz_b = new(Blue.X / Blue.Y, 1, (1 - Blue.X - Blue.Y) / Blue.Y);
        Vector3 xyz_w = new(White.X / White.Y, 1, (1 - White.X - White.Y) / White.Y);
        Matrix4x4 rgb2xyz = new(xyz_r.X, xyz_r.Y, xyz_r.Z, 0,
                                xyz_g.X, xyz_g.Y, xyz_g.Z, 0,
                                xyz_b.X, xyz_b.Y, xyz_b.Z, 0,
                                0, 0, 0, 1);
        Matrix4x4.Invert(rgb2xyz, out rgb2xyz);
        Vector3 s = Vector3.Transform(xyz_w, rgb2xyz);
        rgb2xyz = new(xyz_r.X * s.X, xyz_r.Y * s.X, xyz_r.Z * s.X, 0,
                      xyz_g.X * s.Y, xyz_g.Y * s.Y, xyz_g.Z * s.Y, 0,
                      xyz_b.X * s.Z, xyz_b.Y * s.Z, xyz_b.Z * s.Z, 0,
                      0, 0, 0, 1);
        return rgb2xyz;
    }


    public static Matrix4x4 GetColorTransferMatrix(ColorPrimaries from, ColorPrimaries to)
    {
        Matrix4x4 fromMat = from.GetRGBToXYZMatrix();
        Matrix4x4 toMat = to.GetRGBToXYZMatrix();
        Matrix4x4.Invert(toMat, out toMat);
        return Matrix4x4.Multiply(fromMat, toMat);
    }




    private const float Threshold = 0.0001f;

    private static ColorPrimaries[] DefinedColorPrimaries = [BT709, DisplayP3, BT2020];


    public bool TryGetDefinedPrimaries(out int colorPrimariesId)
    {
        colorPrimariesId = 2;
        foreach (var item in DefinedColorPrimaries)
        {
            if ((this.Red - item.Red).Length() <= Threshold &&
                (this.Green - item.Green).Length() <= Threshold &&
                (this.Blue - item.Blue).Length() <= Threshold &&
                (this.White - item.White).Length() <= Threshold)
            {
                colorPrimariesId = item.Id;
                return true;
            }
        }
        return false;
    }




    public static ColorPrimaries BT709 => new ColorPrimaries
    {
        Id = 1,
        Red = new(0.6400f, 0.3300f),
        Green = new(0.3000f, 0.6000f),
        Blue = new(0.1500f, 0.0600f),
        White = new(0.3127f, 0.3290f)
    };



    public static ColorPrimaries DisplayP3 => new ColorPrimaries
    {
        Id = 12,
        Red = new(0.680f, 0.320f),
        Green = new(0.265f, 0.690f),
        Blue = new(0.150f, 0.060f),
        White = new(0.3127f, 0.3290f)
    };


    public static ColorPrimaries BT2020 => new ColorPrimaries
    {
        Id = 9,
        Red = new(0.708f, 0.292f),
        Green = new(0.170f, 0.797f),
        Blue = new(0.131f, 0.046f),
        White = new(0.3127f, 0.3290f)
    };


    public static ColorPrimaries AdobeRGB => new ColorPrimaries
    {
        Id = 2,
        Red = new(0.6400f, 0.3300f),
        Green = new(0.2100f, 0.7100f),
        Blue = new(0.1500f, 0.0600f),
        White = new(0.3127f, 0.3290f)
    };






}
