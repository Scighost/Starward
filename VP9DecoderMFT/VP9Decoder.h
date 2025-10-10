#pragma once

#include "pch.h"
#include <Windows.h>
#include <mftransform.h>
#include <mfapi.h>
#include <mferror.h>
#include <mfidl.h>
#include <wrl/client.h>
#include <vpx/vpx_decoder.h>
#include <vpx/vp8dx.h>
#include <vpx/vpx_image.h>
#include <vector>

// VP9 支持的输出格式 (基于 Profile)
// Profile 0: 8-bit 4:2:0
// Profile 1: 8-bit 4:2:2, 4:4:4
// Profile 2: 10-bit/12-bit 4:2:0
// Profile 3: 10-bit/12-bit 4:2:2, 4:4:4

// 定义 I422 - 8 - bit 4:2 : 2
static const GUID MFVideoFormat_I422 =
{ 0x32323449, 0x0000, 0x0010, {0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71} };

// 定义 I444 - 8-bit 4:4:4
static const GUID MFVideoFormat_I444 =
{ 0x34343449, 0x0000, 0x0010, {0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71} };


using namespace Microsoft::WRL;

class VP9Decoder : public IMFTransform
{
public:
    VP9Decoder();
    virtual ~VP9Decoder();

    // IUnknown methods
    STDMETHODIMP QueryInterface(REFIID riid, void** ppv) override;
    STDMETHODIMP_(ULONG) AddRef() override;
    STDMETHODIMP_(ULONG) Release() override;

    // IMFTransform methods
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
    HRESULT InitializeDecoder();
    HRESULT ShutdownDecoder();
    HRESULT DecodeFrame(IMFSample* pInputSample, IMFSample** ppOutputSample);
    HRESULT CreateOutputSample(const vpx_image_t* img, IMFSample** ppSample);
    HRESULT ConvertToI420(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI422(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI444(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI420_10bit(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI422_10bit(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI444_10bit(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI420_12bit(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI422_12bit(const vpx_image_t* img, BYTE* pDest);
    HRESULT ConvertToI444_12bit(const vpx_image_t* img, BYTE* pDest);
    HRESULT OnCheckInputType(IMFMediaType* pType);
    HRESULT OnCheckOutputType(IMFMediaType* pType);
    HRESULT OnSetInputType(IMFMediaType* pType);
    HRESULT OnSetOutputType(IMFMediaType* pType);
    HRESULT OnFlush();
    HRESULT OnDrain();
    bool IsValidInputType(IMFMediaType* pType);
    bool IsValidOutputType(IMFMediaType* pType);
    DWORD GetOutputBufferSize(const GUID& subtype, UINT32 width, UINT32 height);
    GUID GetOutputFormatForVpxImage(const vpx_image_t* img);

private:
    LONG m_nRefCount;
    ComPtr<IMFMediaType> m_pInputType;
    ComPtr<IMFMediaType> m_pOutputType;
    ComPtr<IMFSample> m_pPendingSample;
    ComPtr<IMFAttributes> m_pAttributes;

    vpx_codec_ctx_t m_codec;
    bool m_bDecoderInitialized;
    bool m_bStreamingStarted;

    UINT32 m_uiWidth;
    UINT32 m_uiHeight;
    UINT32 m_uiFrameRateNum;
    UINT32 m_uiFrameRateDen;
    GUID m_outputSubtype;

    CRITICAL_SECTION m_critSec;
};