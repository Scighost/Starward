#pragma once

#include "pch.h"
#include <Windows.h>
#include <mftransform.h>
#include <mfapi.h>
#include <mferror.h>
#include <mfidl.h>
#include <wrl/client.h>
#include <vorbis/codec.h>
#include <vector>

using namespace Microsoft::WRL;

// Vorbis 音频格式 GUID — 定义位于 mfuuid.lib，此处仅作声明
// {8D2FD10B-5841-4A6B-8905-588FEC1ADED9}
extern const GUID MFAudioFormat_Vorbis;


class VorbisDecoder : public IMFTransform
{
public:
    VorbisDecoder();
    virtual ~VorbisDecoder();

    // IUnknown
    STDMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    STDMETHODIMP_(ULONG) AddRef() override;
    STDMETHODIMP_(ULONG) Release() override;

    // IMFTransform
    STDMETHODIMP GetStreamLimits(
        DWORD* pdwInputMinimum,
        DWORD* pdwInputMaximum,
        DWORD* pdwOutputMinimum,
        DWORD* pdwOutputMaximum) override;

    STDMETHODIMP GetStreamCount(
        DWORD* pcInputStreams,
        DWORD* pcOutputStreams) override;

    STDMETHODIMP GetStreamIDs(
        DWORD dwInputIDArraySize,
        DWORD* pdwInputIDs,
        DWORD dwOutputIDArraySize,
        DWORD* pdwOutputIDs) override;

    STDMETHODIMP GetInputStreamInfo(
        DWORD dwInputStreamID,
        MFT_INPUT_STREAM_INFO* pStreamInfo) override;

    STDMETHODIMP GetOutputStreamInfo(
        DWORD dwOutputStreamID,
        MFT_OUTPUT_STREAM_INFO* pStreamInfo) override;

    STDMETHODIMP GetAttributes(IMFAttributes** ppAttributes) override;

    STDMETHODIMP GetInputStreamAttributes(
        DWORD dwInputStreamID,
        IMFAttributes** ppAttributes) override;

    STDMETHODIMP GetOutputStreamAttributes(
        DWORD dwOutputStreamID,
        IMFAttributes** ppAttributes) override;

    STDMETHODIMP DeleteInputStream(DWORD dwStreamID) override;

    STDMETHODIMP AddInputStreams(
        DWORD cStreams,
        DWORD* adwStreamIDs) override;

    STDMETHODIMP GetInputAvailableType(
        DWORD dwInputStreamID,
        DWORD dwTypeIndex,
        IMFMediaType** ppType) override;

    STDMETHODIMP GetOutputAvailableType(
        DWORD dwOutputStreamID,
        DWORD dwTypeIndex,
        IMFMediaType** ppType) override;

    STDMETHODIMP SetInputType(
        DWORD dwInputStreamID,
        IMFMediaType* pType,
        DWORD dwFlags) override;

    STDMETHODIMP SetOutputType(
        DWORD dwOutputStreamID,
        IMFMediaType* pType,
        DWORD dwFlags) override;

    STDMETHODIMP GetInputCurrentType(
        DWORD dwInputStreamID,
        IMFMediaType** ppType) override;

    STDMETHODIMP GetOutputCurrentType(
        DWORD dwOutputStreamID,
        IMFMediaType** ppType) override;

    STDMETHODIMP GetInputStatus(
        DWORD dwInputStreamID,
        DWORD* pdwFlags) override;

    STDMETHODIMP GetOutputStatus(DWORD* pdwFlags) override;

    STDMETHODIMP SetOutputBounds(
        LONGLONG hnsLowerBound,
        LONGLONG hnsUpperBound) override;

    STDMETHODIMP ProcessEvent(
        DWORD dwInputStreamID,
        IMFMediaEvent* pEvent) override;

    STDMETHODIMP ProcessMessage(
        MFT_MESSAGE_TYPE eMessage,
        ULONG_PTR ulParam) override;

    STDMETHODIMP ProcessInput(
        DWORD dwInputStreamID,
        IMFSample* pSample,
        DWORD dwFlags) override;

    STDMETHODIMP ProcessOutput(
        DWORD dwFlags,
        DWORD cOutputBufferCount,
        MFT_OUTPUT_DATA_BUFFER* pOutputSamples,
        DWORD* pdwStatus) override;

private:
    // 内部辅助方法
    HRESULT InitializeDecoder(const BYTE* pPrivateData, DWORD cbPrivateData);
    HRESULT ShutdownDecoder();
    HRESULT DecodePacket(IMFSample* pInputSample, IMFSample** ppOutputSample);
    HRESULT CreateOutputSample(const std::vector<float>& pcmData, IMFSample** ppSample);
    HRESULT OnCheckInputType(IMFMediaType* pType);
    HRESULT OnCheckOutputType(IMFMediaType* pType);
    HRESULT OnSetInputType(IMFMediaType* pType);
    HRESULT OnSetOutputType(IMFMediaType* pType);
    HRESULT OnFlush();
    HRESULT OnDrain();

    // 解析 XiphLacing 格式的私有数据，提取各 header packet
    HRESULT ParseXiphLacingHeaders(
        const BYTE* pData,
        DWORD cbData,
        std::vector<std::vector<BYTE>>& headers);

    // 将 libvorbis float PCM 转换为 16-bit PCM
    static void ConvertFloatToInt16(const float* pSrc, INT16* pDst, DWORD nSamples);

private:
    LONG            m_nRefCount;
    ComPtr<IMFMediaType>  m_pInputType;
    ComPtr<IMFMediaType>  m_pOutputType;
    ComPtr<IMFSample>     m_pPendingSample;
    ComPtr<IMFAttributes> m_pAttributes;

    // libvorbis 解码器状态
    vorbis_info       m_vorbisInfo;
    vorbis_comment    m_vorbisComment;
    vorbis_dsp_state  m_vorbisDspState;
    vorbis_block      m_vorbisBlock;

    bool  m_bDecoderInitialized;
    bool  m_bVorbisInfoInited;      // vorbis_info_init / vorbis_comment_init 已调用
    bool  m_bStreamingStarted;
    int   m_nHeaderPacketsReceived; // 内联初始化路径已收到的 header packet 数量

    // 音频属性（从输入媒体类型或 Vorbis 头中获取）
    UINT32  m_uiChannels;
    UINT32  m_uiSampleRate;
    GUID    m_outputSubtype;    // 当前输出子类型
};
