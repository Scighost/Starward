using System.Numerics;
using System.Runtime.InteropServices;

namespace Starward.Codec.ICC;

public static partial class ICCHelper
{

    private const string DllName = "lcms2.dll";



    public static unsafe ColorPrimaries GetColorPrimariesFromIccData(ReadOnlySpan<byte> bytes)
    {
        ColorPrimaries colorPrimaries = new ColorPrimaries();
        fixed (byte* p = bytes)
        {
            using cmsHPROFILE profile = cmsOpenProfileFromMem((IntPtr)p, (uint)bytes.Length);
            if (profile.IsNull)
            {
                throw new Exception("Failed to open ICC profile from memory.");
            }
            cmsCIEXYZ* chad = (cmsCIEXYZ*)cmsReadTag(profile, cmsTagSignature.cmsSigChromaticAdaptationTag);
            Matrix4x4 chadMat = Matrix4x4.Identity;
            if (chad is not null)
            {
                chadMat = new((float)chad->X, (float)(chad + 1)->X, (float)(chad + 2)->X, 0,
                              (float)chad->Y, (float)(chad + 1)->Y, (float)(chad + 2)->Y, 0,
                              (float)chad->Z, (float)(chad + 1)->Z, (float)(chad + 2)->Z, 0,
                              0, 0, 0, 1);
                Matrix4x4.Invert(chadMat, out chadMat);
            }

            cmsCIEXYZ* white = (cmsCIEXYZ*)cmsReadTag(profile, cmsTagSignature.cmsSigMediaWhitePointTag);
            if (white is not null)
            {
                Vector3 whiteVec = new((float)white->X, (float)white->Y, (float)white->Z);
                whiteVec = Vector3.Transform(whiteVec, chadMat);
                double d = whiteVec.X + whiteVec.Y + whiteVec.Z;
                colorPrimaries.White = new((float)(whiteVec.X / d), (float)(whiteVec.Y / d));
            }

            cmsCIExyYTRIPLE* rgb = (cmsCIExyYTRIPLE*)cmsReadTag(profile, cmsTagSignature.cmsSigChromaticityTag);
            if (rgb is not null)
            {
                colorPrimaries.Red = new((float)rgb->Red.x, (float)rgb->Red.y);
                colorPrimaries.Green = new((float)rgb->Green.x, (float)rgb->Green.y);
                colorPrimaries.Blue = new((float)rgb->Blue.x, (float)rgb->Blue.y);
            }
            else
            {
                cmsCIEXYZ* red = (cmsCIEXYZ*)cmsReadTag(profile, cmsTagSignature.cmsSigRedColorantTag);
                if (red is not null)
                {
                    Vector3 redVec = new((float)red->X, (float)red->Y, (float)red->Z);
                    redVec = Vector3.Transform(redVec, chadMat);
                    colorPrimaries.Red = new(redVec.X / (redVec.X + redVec.Y + redVec.Z), redVec.Y / (redVec.X + redVec.Y + redVec.Z));
                }

                cmsCIEXYZ* green = (cmsCIEXYZ*)cmsReadTag(profile, cmsTagSignature.cmsSigGreenColorantTag);
                if (green is not null)
                {
                    Vector3 redVec = new((float)green->X, (float)green->Y, (float)green->Z);
                    redVec = Vector3.Transform(redVec, chadMat);
                    colorPrimaries.Green = new(redVec.X / (redVec.X + redVec.Y + redVec.Z), redVec.Y / (redVec.X + redVec.Y + redVec.Z));
                }

                cmsCIEXYZ* blue = (cmsCIEXYZ*)cmsReadTag(profile, cmsTagSignature.cmsSigBlueColorantTag);
                if (blue is not null)
                {
                    Vector3 blueVec = new((float)blue->X, (float)blue->Y, (float)blue->Z);
                    blueVec = Vector3.Transform(blueVec, chadMat);
                    colorPrimaries.Blue = new(blueVec.X / (blueVec.X + blueVec.Y + blueVec.Z), blueVec.Y / (blueVec.X + blueVec.Y + blueVec.Z));
                }

            }
        }
        return colorPrimaries;
    }


    public static unsafe byte[] CreateSRGBIccData()
    {
        using cmsHPROFILE profile = cmsCreate_sRGBProfile();
        if (profile.IsNull)
        {
            throw new Exception("Failed to create sRGB ICC profile.");
        }
        uint size = 0;
        if (cmsSaveProfileToMem(profile, IntPtr.Zero, ref size))
        {
            byte[] data = new byte[size];
            fixed (byte* p = data)
            {
                if (cmsSaveProfileToMem(profile, (IntPtr)p, ref size))
                {
                    return data;
                }
            }
        }
        throw new Exception("Failed to save sRGB ICC profile to memory.");
    }


    public static unsafe byte[] CreateIccData(ColorPrimaries colorPrimaries)
    {
        if (colorPrimaries.Id == 1)
        {
            return CreateSRGBIccData();
        }
        else
        {
            cmsCIExyY white = new cmsCIExyY { x = colorPrimaries.White.X, y = colorPrimaries.White.Y, Y = 1 };
            cmsCIExyYTRIPLE rgb = new cmsCIExyYTRIPLE
            {
                Red = new cmsCIExyY { x = colorPrimaries.Red.X, y = colorPrimaries.Red.Y, Y = 1 },
                Green = new cmsCIExyY { x = colorPrimaries.Green.X, y = colorPrimaries.Green.Y, Y = 1 },
                Blue = new cmsCIExyY { x = colorPrimaries.Blue.X, y = colorPrimaries.Blue.Y, Y = 1 },
            };
            using cmsToneCurve srgbCurve = cmsBuildParametricToneCurve(IntPtr.Zero, 4, [2.4, 1.0 / 1.055, 0.055 / 1.055, 1.0 / 12.92, 0.04045]);
            using cmsHPROFILE profile = cmsCreateRGBProfile(&white, &rgb, [srgbCurve, srgbCurve, srgbCurve]);

            uint size = 0;
            if (cmsSaveProfileToMem(profile, IntPtr.Zero, ref size))
            {
                byte[] data = new byte[size];
                fixed (byte* p = data)
                {
                    if (cmsSaveProfileToMem(profile, (IntPtr)p, ref size))
                    {
                        return data;
                    }
                }
            }
            throw new Exception("Failed to save ICC profile to memory.");
        }
    }



    [LibraryImport(DllName)]
    private static partial cmsHPROFILE cmsOpenProfileFromMem(IntPtr memPtr, uint size);


    [LibraryImport(DllName)]
    private static partial cmsBool cmsCloseProfile(cmsHPROFILE hPROFILE);


    [LibraryImport(DllName)]
    private static unsafe partial void* cmsReadTag(cmsHPROFILE hProfile, cmsTagSignature sig);


    [LibraryImport(DllName)]
    private static unsafe partial cmsBool cmsWriteTag(cmsHPROFILE hProfile, cmsTagSignature sig, void* data);


    [LibraryImport(DllName)]
    private static unsafe partial cmsHPROFILE cmsCreate_sRGBProfile();


    [LibraryImport(DllName)]
    private static partial cmsBool cmsSaveProfileToMem(cmsHPROFILE hProfile, IntPtr MemPtr, ref uint BytesNeeded);


    [LibraryImport(DllName)]
    private static partial cmsToneCurve cmsBuildParametricToneCurve(IntPtr ContextID, int Type, double[] Params);


    [LibraryImport(DllName)]
    private static partial void cmsFreeToneCurve(cmsToneCurve Curve);


    [LibraryImport(DllName)]
    private static unsafe partial cmsHPROFILE cmsCreateRGBProfile(cmsCIExyY* WhitePoint, cmsCIExyYTRIPLE* Primaries, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] IntPtr[] TransferFunction);


    [StructLayout(LayoutKind.Sequential)]
    private record struct cmsHPROFILE : IDisposable
    {
        private IntPtr ptr;

        public bool IsNull => ptr == IntPtr.Zero;


        public static implicit operator IntPtr(cmsHPROFILE value) => value.ptr;

        public static implicit operator cmsHPROFILE(IntPtr value) => new() { ptr = value };

        public void Dispose()
        {
            cmsCloseProfile(this);
        }

    }


    [StructLayout(LayoutKind.Sequential)]
    private record struct cmsToneCurve : IDisposable
    {
        private IntPtr ptr;

        public bool IsNull => ptr == IntPtr.Zero;


        public static implicit operator IntPtr(cmsToneCurve value) => value.ptr;

        public static implicit operator cmsToneCurve(IntPtr value) => new() { ptr = value };

        public void Dispose()
        {
            cmsFreeToneCurve(this);
        }

    }




    private static Matrix4x4 Bradford = new Matrix4x4(0.8951f, -0.7502f, 0.0389f, 0,
                                                      0.2664f, 1.7135f, -0.0685f, 0,
                                                      -0.1614f, 0.0367f, 1.0296f, 0,
                                                      0, 0, 0, 1);



    private static Matrix4x4 Bradford_Invert = new Matrix4x4(0.9869929f, 0.4323053f, -0.0085287f, 0,
                                                        -0.1470543f, 0.5183603f, 0.0400428f, 0,
                                                        0.1599627f, 0.0492912f, 0.9684867f, 0,
                                                        0, 0, 0, 1);

    private static Vector2 xy_D65 = new(0.3127f, 0.3290f);

    private static Vector3 XYZ_D50 = new(0.9642f, 1.0000f, 0.8251f);


}
