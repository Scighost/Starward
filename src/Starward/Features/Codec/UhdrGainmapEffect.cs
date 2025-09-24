using ComputeSharp;
using ComputeSharp.D2D1;
using ComputeSharp.D2D1.WinUI;
using Microsoft.Graphics.Canvas;
using System;
using Windows.Graphics.Effects;

namespace Starward.Features.Codec;


public partial class UhdrPixelGainEffect : CanvasEffect
{

    public IGraphicsEffectSource SdrSource { get; set; }

    public IGraphicsEffectSource HdrSource { get; set; }

    public CanvasBufferPrecision? BufferPrecision { get; set; }


    protected override void BuildEffectGraph(CanvasEffectGraph effectGraph)
    {
        PixelShaderEffect<UhdrPixelGainShader> effect = new()
        {
            BufferPrecision = BufferPrecision,
        };
        effect.Sources[0] = SdrSource;
        effect.Sources[1] = HdrSource;
        effectGraph.RegisterOutputNode(effect);
    }

    protected override void ConfigureEffectGraph(CanvasEffectGraph effectGraph)
    {

    }

}


public partial class UhdrGainmapEffect : CanvasEffect
{

    public float3 MinContentBoost { get; set; }

    public float3 MaxContentBoost { get; set; }

    public IGraphicsEffectSource PixelGainSource { get; set; }

    public CanvasBufferPrecision? BufferPrecision { get; set; }


    protected override void BuildEffectGraph(CanvasEffectGraph effectGraph)
    {
        PixelShaderEffect<UhdrGainmapShader> effect = new()
        {
            BufferPrecision = BufferPrecision,
        };
        effect.Sources[0] = PixelGainSource;
        effect.ConstantBuffer = new UhdrGainmapShader(Log2(MinContentBoost), Log2(MaxContentBoost));
        effectGraph.RegisterOutputNode(effect);
    }

    protected override void ConfigureEffectGraph(CanvasEffectGraph effectGraph)
    {

    }

    private static float3 Log2(float3 value)
    {
        return new float3(MathF.Log2(value.R), MathF.Log2(value.G), MathF.Log2(value.B));
    }

}





[D2DInputCount(2)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct UhdrPixelGainShader : ID2D1PixelShader
{

    private const float OFFSET = 0.015625f;

    public float4 Execute()
    {
        float4 sdr = D2D.GetInput(0);
        float4 hdr = D2D.GetInput(1);
        float3 gain = (Hlsl.Max(hdr.RGB, 0) + OFFSET) / (Hlsl.Max(sdr.RGB, 0) + OFFSET);
        return new float4(gain, sdr.A);
    }

}


[D2DInputCount(1)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct UhdrGainmapShader(float3 minBoostLog, float3 maxBoostLog) : ID2D1PixelShader
{

    public float4 Execute()
    {
        float4 gain = D2D.GetInput(0);
        float3 logGain = (Hlsl.Log2(gain.RGB) - minBoostLog) / (maxBoostLog - minBoostLog);
        return new float4(Hlsl.Clamp(logGain, 0, 1), gain.A);
    }

}
