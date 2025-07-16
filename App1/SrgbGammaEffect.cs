using ComputeSharp;
using ComputeSharp.D2D1;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Effects;

namespace Starward.Features.Screenshot;


// https://en.wikipedia.org/wiki/SRGB
public partial class SrgbGammaEffect : CanvasEffect
{

    public SrgbGammaMode GammaMode { get; set; }

    public IGraphicsEffectSource Source { get; set; }

    public CanvasBufferPrecision? BufferPrecision { get; set; }


    protected override void BuildEffectGraph(CanvasEffectGraph effectGraph)
    {
        if (GammaMode is SrgbGammaMode.EOTF)
        {
            PixelShaderEffect<SrgbEOTFShader> effect = new()
            {
                BufferPrecision = this.BufferPrecision,
            };
            effect.Sources[0] = this.Source;
            effectGraph.RegisterOutputNode(effect);
        }
        else if (GammaMode is SrgbGammaMode.OETF)
        {
            PixelShaderEffect<SrgbOETFShader> effect = new()
            {
                BufferPrecision = this.BufferPrecision,
            };
            effect.Sources[0] = this.Source;
            effectGraph.RegisterOutputNode(effect);
        }
        else
        {
            PixelShaderEffect<PQEOTFShader> effect = new()
            {
                BufferPrecision = this.BufferPrecision,
            };
            effect.Sources[0] = this.Source;
            effectGraph.RegisterOutputNode(effect);
        }
    }


    protected override void ConfigureEffectGraph(CanvasEffectGraph effectGraph)
    {

    }

}



public enum SrgbGammaMode
{
    EOTF = 0,
    OETF = 1,
    PQEOTF = 2,
}





[D2DInputCount(1)]
[D2DInputSimple(0)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct SrgbEOTFShader : ID2D1PixelShader
{

    public float4 Execute()
    {
        float4 color = D2D.GetInput(0);
        float3 rgb = color.RGB;
        float3 isLow = Hlsl.Step(rgb, 0.04045f);
        rgb = Hlsl.Lerp(Hlsl.Pow(Hlsl.Abs((rgb + 0.055f) / 1.055f), 2.4f), rgb / 12.92f, isLow);
        return new float4(rgb, color.A);
    }

}


[D2DInputCount(1)]
[D2DInputSimple(0)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct SrgbOETFShader : ID2D1PixelShader
{

    public float4 Execute()
    {
        float4 color = D2D.GetInput(0);
        float3 rgb = color.RGB;
        float3 isLow = Hlsl.Step(rgb, 0.0031308f);
        rgb = Hlsl.Lerp(1.055f * Hlsl.Pow(Hlsl.Abs(rgb), 1 / 2.4f) - 0.055f, rgb * 12.92f, isLow);
        return new float4(rgb, color.A);
    }

}

[D2DInputCount(1)]
[D2DInputSimple(0)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct PQEOTFShader : ID2D1PixelShader
{

    const float m1 = 2610f / 16384;
    const float m2 = 2523f / 4096 * 128;
    const float c1 = 3424f / 4096;
    const float c2 = 2413f / 4096 * 32;
    const float c3 = 2392f / 4096 * 32;


    public float4 Execute()
    {
        float4 color = D2D.GetInput(0);
        float3 rgb = color.RGB;
        float3 isLow = Hlsl.Step(rgb, 0.0031308f);
        rgb = Hlsl.Lerp(1.055f * Hlsl.Pow(Hlsl.Abs(rgb), 1 / 2.4f) - 0.055f, rgb * 12.92f, isLow);
        return new float4(rgb, color.A);

        float3 N_pow = Hlsl.Pow(rgb, 1f / m2);
        float3 numerator = Hlsl.Max(N_pow - c1, 0);
        float3 denominator = c2 - c3 * N_pow;
        float3 L = Hlsl.Pow(numerator / denominator, 1f / m1);

        return new float4(L / 80f, color.A);
    }

}