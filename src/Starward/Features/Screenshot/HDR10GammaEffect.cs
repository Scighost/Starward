using ComputeSharp;
using ComputeSharp.D2D1;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Graphics.Effects;

namespace Starward.Features.Screenshot;

internal partial class ScRGBToHDR10Effect : CanvasEffect
{

    public IGraphicsEffectSource Source { get; set; }

    public CanvasBufferPrecision? BufferPrecision { get; set; }


    protected override void BuildEffectGraph(CanvasEffectGraph effectGraph)
    {
        var colorEffect = new ColorMatrixEffect
        {
            Source = Source,
            ColorMatrix = BT709ToBT2020ColorMatrix,
            BufferPrecision = this.BufferPrecision,
        };
        var pqEffect = new PixelShaderEffect<PQOETFShader>()
        {
            ConstantBuffer = new PQOETFShader(125),
            BufferPrecision = this.BufferPrecision,
        };
        pqEffect.Sources[0] = colorEffect;
        effectGraph.RegisterOutputNode(pqEffect);
    }


    protected override void ConfigureEffectGraph(CanvasEffectGraph effectGraph)
    {

    }



    private static Matrix5x4 BT709ToBT2020ColorMatrix = new(
        0.6284037f, 0.0644736f, 0.0164828f, 0f,
        0.3298326f, 0.9171785f, 0.0880559f, 0f,
        0.0433918f, 0.0110605f, 0.8955600f, 0f,
        0f, 0f, 0f, 1f,
        0f, 0f, 0f, 0f);


    [D2DInputCount(1)]
    [D2DInputSimple(0)]
    [D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
    [D2DGeneratedPixelShaderDescriptor]
    internal readonly partial struct PQOETFShader(float maxLevel = 10000) : ID2D1PixelShader
    {

        const float c1 = 107f / 128f;
        const float c2 = 2413f / 128f;
        const float c3 = 2392f / 128f;
        const float m = 2523f / 32f;
        const float n = 1305f / 8192f;

        public float4 Execute()
        {
            float4 color = D2D.GetInput(0);
            float3 rgb = Hlsl.Abs(color.RGB / maxLevel);

            float3 v1 = c1 + c2 * Hlsl.Pow(rgb, n);
            float3 v2 = 1 + c3 * Hlsl.Pow(rgb, n);
            rgb = Hlsl.Pow(v1 / v2, m);
            return new float4(Hlsl.Saturate(rgb), color.A);
        }

    }

}



internal partial class HDR10ToScRGBEffect : CanvasEffect
{

    public IGraphicsEffectSource Source { get; set; }

    public CanvasBufferPrecision? BufferPrecision { get; set; }


    protected override void BuildEffectGraph(CanvasEffectGraph effectGraph)
    {
        var pqEffect = new PixelShaderEffect<PQEOTFShader>()
        {
            ConstantBuffer = new PQEOTFShader(125),
            BufferPrecision = this.BufferPrecision,
        };
        pqEffect.Sources[0] = Source;
        var colorEffect = new ColorMatrixEffect
        {
            Source = pqEffect,
            ColorMatrix = BT2020ToBT709ColorMatrix,
            BufferPrecision = this.BufferPrecision,
        };
        effectGraph.RegisterOutputNode(colorEffect);
    }


    protected override void ConfigureEffectGraph(CanvasEffectGraph effectGraph)
    {

    }



    private static Matrix5x4 BT2020ToBT709ColorMatrix = new(
         1.6535364f, -0.1160068f, -0.0190270f, 0f,
        -0.5876440f, 1.1328219f, -0.1005690f, 0f,
        -0.0728597f, -0.0083700f, 1.1187837f, 0f,
        0f, 0f, 0f, 1f,
        0f, 0f, 0f, 0f);


    [D2DInputCount(1)]
    [D2DInputSimple(0)]
    [D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
    [D2DGeneratedPixelShaderDescriptor]
    internal readonly partial struct PQEOTFShader(float maxLevel = 10000) : ID2D1PixelShader
    {

        const float c1 = 107f / 128f;
        const float c2 = 2413f / 128f;
        const float c3 = 2392f / 128f;
        const float m = 2523f / 32f;
        const float n = 1305f / 8192f;

        public float4 Execute()
        {
            float4 color = D2D.GetInput(0);
            float3 rgb = Hlsl.Abs(color.RGB);

            float3 v1 = Hlsl.Max(Hlsl.Pow(rgb, 1 / m) - c1, 0);
            float3 v2 = c2 - c3 * Hlsl.Pow(rgb, 1 / m);
            rgb = maxLevel * Hlsl.Pow(Hlsl.Abs(v1 / v2), 1 / n);
            return new float4(rgb, color.A);
        }

    }

}