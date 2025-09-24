using ComputeSharp;
using ComputeSharp.D2D1;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Windows.Graphics.Effects;

namespace Starward.Features.Codec;


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
        else
        {
            PixelShaderEffect<SrgbOETFShader> effect = new()
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

}



public enum SrgbGammaMode
{
    EOTF = 0,
    OETF = 1,
}

