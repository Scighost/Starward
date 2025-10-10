#include "pch.h"
#include "VP9Decoder.h"
#include <mfapi.h>


static const GUID g_OutputFormats[] = {
    MFVideoFormat_IYUV,   // 8-bit 4:2:0 (Profile 0)
    MFVideoFormat_I422,   // 8-bit 4:2:2 (Profile 1)
    MFVideoFormat_I444,   // 8-bit 4:4:4 (Profile 1)
    MFVideoFormat_P010,   // 10-bit 4:2:0 (Profile 2)
    MFVideoFormat_P210,   // 10-bit 4:2:2 (Profile 3)
    MFVideoFormat_Y410,   // 10-bit 4:4:4 (Profile 3)
    MFVideoFormat_P016,   // 12-bit 4:2:0 (Profile 2)
    MFVideoFormat_P216,   // 12-bit 4:2:2 (Profile 3)
    MFVideoFormat_Y416    // 12-bit 4:4:4 (Profile 3)
};

static const DWORD g_NumOutputFormats = ARRAYSIZE(g_OutputFormats);

VP9Decoder::VP9Decoder()
    : m_nRefCount(1)
    , m_bDecoderInitialized(false)
    , m_bStreamingStarted(false)
    , m_uiWidth(0)
    , m_uiHeight(0)
    , m_uiFrameRateNum(30)
    , m_uiFrameRateDen(1)
    , m_outputSubtype(GUID_NULL)
{
    InitializeCriticalSection(&m_critSec);
    ZeroMemory(&m_codec, sizeof(m_codec));
    MFCreateAttributes(&m_pAttributes, 3);
}

VP9Decoder::~VP9Decoder()
{
    ShutdownDecoder();
    DeleteCriticalSection(&m_critSec);
}

// IUnknown methods
STDMETHODIMP VP9Decoder::QueryInterface(REFIID riid, void** ppv)
{
    if (!ppv)
        return E_POINTER;

    if (riid == IID_IUnknown)
    {
        *ppv = static_cast<IUnknown*>(this);
    }
    else if (riid == IID_IMFTransform)
    {
        *ppv = static_cast<IMFTransform*>(this);
    }
    else
    {
        *ppv = NULL;
        return E_NOINTERFACE;
    }

    AddRef();
    return S_OK;
}

STDMETHODIMP_(ULONG) VP9Decoder::AddRef()
{
    return InterlockedIncrement(&m_nRefCount);
}

STDMETHODIMP_(ULONG) VP9Decoder::Release()
{
    ULONG count = InterlockedDecrement(&m_nRefCount);
    if (count == 0)
    {
        delete this;
    }
    return count;
}

// IMFTransform methods
STDMETHODIMP VP9Decoder::GetStreamLimits(
    DWORD* pdwInputMinimum,
    DWORD* pdwInputMaximum,
    DWORD* pdwOutputMinimum,
    DWORD* pdwOutputMaximum)
{
    if (!pdwInputMinimum || !pdwInputMaximum || !pdwOutputMinimum || !pdwOutputMaximum)
        return E_POINTER;

    *pdwInputMinimum = 1;
    *pdwInputMaximum = 1;
    *pdwOutputMinimum = 1;
    *pdwOutputMaximum = 1;

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetStreamCount(DWORD* pcInputStreams, DWORD* pcOutputStreams)
{
    if (!pcInputStreams || !pcOutputStreams)
        return E_POINTER;

    *pcInputStreams = 1;
    *pcOutputStreams = 1;

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetStreamIDs(
    DWORD dwInputIDArraySize,
    DWORD* pdwInputIDs,
    DWORD dwOutputIDArraySize,
    DWORD* pdwOutputIDs)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::GetInputStreamInfo(DWORD dwInputStreamID, MFT_INPUT_STREAM_INFO* pStreamInfo)
{
    if (dwInputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!pStreamInfo)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);

    pStreamInfo->dwFlags = MFT_INPUT_STREAM_WHOLE_SAMPLES |
        MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER;
    pStreamInfo->cbSize = 0;
    pStreamInfo->cbMaxLookahead = 0;
    pStreamInfo->cbAlignment = 0;
    pStreamInfo->hnsMaxLatency = 0;

    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetOutputStreamInfo(DWORD dwOutputStreamID, MFT_OUTPUT_STREAM_INFO* pStreamInfo)
{
    if (dwOutputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!pStreamInfo)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);

    pStreamInfo->dwFlags = MFT_OUTPUT_STREAM_WHOLE_SAMPLES |
        MFT_OUTPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER |
        MFT_OUTPUT_STREAM_FIXED_SAMPLE_SIZE |
        MFT_OUTPUT_STREAM_PROVIDES_SAMPLES;

    if (m_pOutputType && m_uiWidth > 0 && m_uiHeight > 0)
    {
        pStreamInfo->cbSize = GetOutputBufferSize(m_outputSubtype, m_uiWidth, m_uiHeight);
    }
    else
    {
        pStreamInfo->cbSize = 0;
    }

    pStreamInfo->cbAlignment = 0;

    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetAttributes(IMFAttributes** ppAttributes)
{
    if (!ppAttributes)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);
    *ppAttributes = m_pAttributes.Get();
    if (*ppAttributes)
    {
        (*ppAttributes)->AddRef();
    }
    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetInputStreamAttributes(DWORD dwInputStreamID, IMFAttributes** ppAttributes)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::GetOutputStreamAttributes(DWORD dwOutputStreamID, IMFAttributes** ppAttributes)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::DeleteInputStream(DWORD dwStreamID)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::AddInputStreams(DWORD cStreams, DWORD* adwStreamIDs)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::GetInputAvailableType(
    DWORD dwInputStreamID,
    DWORD dwTypeIndex,
    IMFMediaType** ppType)
{
    if (dwInputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!ppType)
        return E_POINTER;

    if (dwTypeIndex != 0)
        return MF_E_NO_MORE_TYPES;

    HRESULT hr = S_OK;
    ComPtr<IMFMediaType> pType;

    hr = MFCreateMediaType(&pType);
    if (FAILED(hr))
        return hr;

    hr = pType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
    if (FAILED(hr))
        return hr;

    hr = pType->SetGUID(MF_MT_SUBTYPE, MFVideoFormat_VP90);
    if (FAILED(hr))
        return hr;

    *ppType = pType.Detach();

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetOutputAvailableType(
    DWORD dwOutputStreamID,
    DWORD dwTypeIndex,
    IMFMediaType** ppType)
{
    if (dwOutputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!ppType)
        return E_POINTER;

    if (!m_pInputType)
        return MF_E_TRANSFORM_TYPE_NOT_SET;

    if (dwTypeIndex >= g_NumOutputFormats)
        return MF_E_NO_MORE_TYPES;

    HRESULT hr = S_OK;
    ComPtr<IMFMediaType> pType;

    hr = MFCreateMediaType(&pType);
    if (FAILED(hr))
        return hr;

    hr = pType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
    if (FAILED(hr))
        return hr;

    hr = pType->SetGUID(MF_MT_SUBTYPE, g_OutputFormats[dwTypeIndex]);
    if (FAILED(hr))
        return hr;

    if (m_uiWidth > 0 && m_uiHeight > 0)
    {
        hr = MFSetAttributeSize(pType.Get(), MF_MT_FRAME_SIZE, m_uiWidth, m_uiHeight);
        if (FAILED(hr))
            return hr;

        hr = MFSetAttributeRatio(pType.Get(), MF_MT_FRAME_RATE, m_uiFrameRateNum, m_uiFrameRateDen);
        if (FAILED(hr))
            return hr;

        hr = MFSetAttributeRatio(pType.Get(), MF_MT_PIXEL_ASPECT_RATIO, 1, 1);
        if (FAILED(hr))
            return hr;

        hr = pType->SetUINT32(MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);
        if (FAILED(hr))
            return hr;
    }

    *ppType = pType.Detach();

    return S_OK;
}

STDMETHODIMP VP9Decoder::SetInputType(DWORD dwInputStreamID, IMFMediaType* pType, DWORD dwFlags)
{
    if (dwInputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (dwFlags & ~MFT_SET_TYPE_TEST_ONLY)
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    EnterCriticalSection(&m_critSec);

    if (m_pPendingSample)
    {
        hr = MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;
        goto done;
    }

    if (pType)
    {
        hr = OnCheckInputType(pType);
        if (FAILED(hr))
            goto done;
    }

    if (!(dwFlags & MFT_SET_TYPE_TEST_ONLY))
    {
        if (pType)
        {
            hr = OnSetInputType(pType);
        }
        else
        {
            m_pInputType.Reset();
            ShutdownDecoder();
        }
    }

done:
    LeaveCriticalSection(&m_critSec);
    return hr;
}

STDMETHODIMP VP9Decoder::SetOutputType(DWORD dwOutputStreamID, IMFMediaType* pType, DWORD dwFlags)
{
    if (dwOutputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (dwFlags & ~MFT_SET_TYPE_TEST_ONLY)
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    EnterCriticalSection(&m_critSec);

    if (m_pPendingSample)
    {
        hr = MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;
        goto done;
    }

    if (pType)
    {
        hr = OnCheckOutputType(pType);
        if (FAILED(hr))
            goto done;
    }

    if (!(dwFlags & MFT_SET_TYPE_TEST_ONLY))
    {
        if (pType)
        {
            hr = OnSetOutputType(pType);
        }
        else
        {
            m_pOutputType.Reset();
            m_outputSubtype = GUID_NULL;
        }
    }

done:
    LeaveCriticalSection(&m_critSec);
    return hr;
}

STDMETHODIMP VP9Decoder::GetInputCurrentType(DWORD dwInputStreamID, IMFMediaType** ppType)
{
    if (dwInputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!ppType)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);

    if (!m_pInputType)
    {
        LeaveCriticalSection(&m_critSec);
        return MF_E_TRANSFORM_TYPE_NOT_SET;
    }

    *ppType = m_pInputType.Get();
    (*ppType)->AddRef();

    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetOutputCurrentType(DWORD dwOutputStreamID, IMFMediaType** ppType)
{
    if (dwOutputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!ppType)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);

    if (!m_pOutputType)
    {
        LeaveCriticalSection(&m_critSec);
        return MF_E_TRANSFORM_TYPE_NOT_SET;
    }

    *ppType = m_pOutputType.Get();
    (*ppType)->AddRef();

    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetInputStatus(DWORD dwInputStreamID, DWORD* pdwFlags)
{
    if (dwInputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (!pdwFlags)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);

    if (m_pPendingSample)
    {
        *pdwFlags = 0;
    }
    else
    {
        *pdwFlags = MFT_INPUT_STATUS_ACCEPT_DATA;
    }

    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::GetOutputStatus(DWORD* pdwFlags)
{
    if (!pdwFlags)
        return E_POINTER;

    EnterCriticalSection(&m_critSec);

    if (m_pPendingSample)
    {
        *pdwFlags = MFT_OUTPUT_STATUS_SAMPLE_READY;
    }
    else
    {
        *pdwFlags = 0;
    }

    LeaveCriticalSection(&m_critSec);

    return S_OK;
}

STDMETHODIMP VP9Decoder::SetOutputBounds(LONGLONG hnsLowerBound, LONGLONG hnsUpperBound)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::ProcessEvent(DWORD dwInputStreamID, IMFMediaEvent* pEvent)
{
    return E_NOTIMPL;
}

STDMETHODIMP VP9Decoder::ProcessMessage(MFT_MESSAGE_TYPE eMessage, ULONG_PTR ulParam)
{
    HRESULT hr = S_OK;

    EnterCriticalSection(&m_critSec);

    switch (eMessage)
    {
    case MFT_MESSAGE_COMMAND_FLUSH:
        hr = OnFlush();
        break;

    case MFT_MESSAGE_COMMAND_DRAIN:
        hr = OnDrain();
        break;

    case MFT_MESSAGE_SET_D3D_MANAGER:
        hr = S_OK;
        break;

    case MFT_MESSAGE_NOTIFY_BEGIN_STREAMING:
        m_bStreamingStarted = true;
        break;

    case MFT_MESSAGE_NOTIFY_END_STREAMING:
        m_bStreamingStarted = false;
        break;

    case MFT_MESSAGE_NOTIFY_START_OF_STREAM:
    case MFT_MESSAGE_NOTIFY_END_OF_STREAM:
        hr = S_OK;
        break;

    default:
        hr = E_NOTIMPL;
        break;
    }

    LeaveCriticalSection(&m_critSec);

    return hr;
}

STDMETHODIMP VP9Decoder::ProcessInput(DWORD dwInputStreamID, IMFSample* pSample, DWORD dwFlags)
{
    if (dwInputStreamID != 0)
        return MF_E_INVALIDSTREAMNUMBER;

    if (dwFlags != 0)
        return E_INVALIDARG;

    if (!pSample)
        return E_POINTER;

    HRESULT hr = S_OK;

    EnterCriticalSection(&m_critSec);

    if (!m_pInputType || !m_pOutputType)
    {
        hr = MF_E_NOTACCEPTING;
        goto done;
    }

    if (m_pPendingSample)
    {
        hr = MF_E_NOTACCEPTING;
        goto done;
    }

    if (!m_bDecoderInitialized)
    {
        hr = InitializeDecoder();
        if (FAILED(hr))
            goto done;
    }

    m_pPendingSample = pSample;

done:
    LeaveCriticalSection(&m_critSec);
    return hr;
}

STDMETHODIMP VP9Decoder::ProcessOutput(
    DWORD dwFlags,
    DWORD cOutputBufferCount,
    MFT_OUTPUT_DATA_BUFFER* pOutputSamples,
    DWORD* pdwStatus)
{
    if (dwFlags != 0)
        return E_INVALIDARG;

    if (cOutputBufferCount != 1 || !pOutputSamples || !pdwStatus)
        return E_INVALIDARG;

    HRESULT hr = S_OK;

    EnterCriticalSection(&m_critSec);

    if (!m_pInputType || !m_pOutputType)
    {
        hr = MF_E_TRANSFORM_TYPE_NOT_SET;
        goto done;
    }

    if (!m_pPendingSample)
    {
        hr = MF_E_TRANSFORM_NEED_MORE_INPUT;
        goto done;
    }

    pOutputSamples[0].dwStatus = 0;
    pOutputSamples[0].pEvents = NULL;
    pOutputSamples[0].pSample = NULL;

    hr = DecodeFrame(m_pPendingSample.Get(), &pOutputSamples[0].pSample);

    if (SUCCEEDED(hr))
    {
        m_pPendingSample.Reset();
    }

    *pdwStatus = 0;

done:
    LeaveCriticalSection(&m_critSec);
    return hr;
}

// Private helper methods
HRESULT VP9Decoder::InitializeDecoder()
{
    if (m_bDecoderInitialized)
        return S_OK;

    vpx_codec_iface_t* iface = vpx_codec_vp9_dx();
    vpx_codec_dec_cfg_t cfg;
    memset(&cfg, 0, sizeof(cfg));
    cfg.threads = 4;

    vpx_codec_err_t err = vpx_codec_dec_init(&m_codec, iface, &cfg, 0);
    if (err != VPX_CODEC_OK)
    {
        return E_FAIL;
    }

    m_bDecoderInitialized = true;
    return S_OK;
}

HRESULT VP9Decoder::ShutdownDecoder()
{
    if (m_bDecoderInitialized)
    {
        vpx_codec_destroy(&m_codec);
        m_bDecoderInitialized = false;
    }
    return S_OK;
}

HRESULT VP9Decoder::DecodeFrame(IMFSample* pInputSample, IMFSample** ppOutputSample)
{
    if (!pInputSample || !ppOutputSample)
        return E_POINTER;

    HRESULT hr = S_OK;
    ComPtr<IMFMediaBuffer> pBuffer;
    BYTE* pData = NULL;
    DWORD cbData = 0;

    hr = pInputSample->ConvertToContiguousBuffer(&pBuffer);
    if (FAILED(hr))
        return hr;

    hr = pBuffer->Lock(&pData, NULL, &cbData);
    if (FAILED(hr))
        return hr;

    vpx_codec_err_t err = vpx_codec_decode(&m_codec, pData, cbData, NULL, 0);

    pBuffer->Unlock();

    if (err != VPX_CODEC_OK)
    {
        return E_FAIL;
    }

    vpx_codec_iter_t iter = NULL;
    vpx_image_t* img = vpx_codec_get_frame(&m_codec, &iter);

    if (!img)
    {
        return MF_E_TRANSFORM_NEED_MORE_INPUT;
    }

    // 使用实际解码出来的图像尺寸，而不是从媒体类型获取的尺寸
    UINT32 actualWidth = img->d_w;
    UINT32 actualHeight = img->d_h;

    // 如果尺寸变化了，更新内部状态
    if (m_uiWidth != actualWidth || m_uiHeight != actualHeight)
    {
        m_uiWidth = actualWidth;
        m_uiHeight = actualHeight;

        // 更新输出媒体类型
        if (m_pOutputType)
        {
            MFSetAttributeSize(m_pOutputType.Get(), MF_MT_FRAME_SIZE, m_uiWidth, m_uiHeight);
        }
    }

    hr = CreateOutputSample(img, ppOutputSample);
    if (FAILED(hr))
        return hr;

    LONGLONG timestamp = 0;
    if (SUCCEEDED(pInputSample->GetSampleTime(&timestamp)))
    {
        (*ppOutputSample)->SetSampleTime(timestamp);
    }

    LONGLONG duration = 0;
    if (SUCCEEDED(pInputSample->GetSampleDuration(&duration)))
    {
        (*ppOutputSample)->SetSampleDuration(duration);
    }

    return S_OK;
}

GUID VP9Decoder::GetOutputFormatForVpxImage(const vpx_image_t* img)
{
    // 根据 vpx_image 的格式选择对应的输出格式
    switch (img->fmt)
    {
    case VPX_IMG_FMT_I420:
    case VPX_IMG_FMT_YV12:
        return MFVideoFormat_IYUV;  // 8-bit 4:2:0
    case VPX_IMG_FMT_I422:
        return MFVideoFormat_I422;   // 8-bit 4:2:2
    case VPX_IMG_FMT_I444:
        return MFVideoFormat_I444;   // 8-bit 4:4:4
    case VPX_IMG_FMT_I42016:
        return MFVideoFormat_P010;   // 10-bit 4:2:0
    case VPX_IMG_FMT_I42216:
        return MFVideoFormat_P210;   // 10-bit 4:2:2
    case VPX_IMG_FMT_I44416:
        return MFVideoFormat_Y410;   // 10-bit 4:4:4
    default:
        return MFVideoFormat_IYUV;   // 默认 8-bit 4:2:0
    }
}

HRESULT VP9Decoder::CreateOutputSample(const vpx_image_t* img, IMFSample** ppSample)
{
    if (!img || !ppSample)
        return E_POINTER;

    HRESULT hr = S_OK;
    ComPtr<IMFSample> pSample;
    ComPtr<IMFMediaBuffer> pBuffer;
    BYTE* pDest = NULL;

    // 使用实际图像的尺寸
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;
    DWORD cbBuffer = GetOutputBufferSize(m_outputSubtype, width, height);

    hr = MFCreateSample(&pSample);
    if (FAILED(hr))
        return hr;

    hr = MFCreateMemoryBuffer(cbBuffer, &pBuffer);
    if (FAILED(hr))
        return hr;

    hr = pBuffer->Lock(&pDest, NULL, NULL);
    if (FAILED(hr))
        return hr;

    // 根据输出格式和图像格式进行转换
    if (m_outputSubtype == MFVideoFormat_IYUV)
    {
        hr = ConvertToI420(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_I422)
    {
        hr = ConvertToI422(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_I444)
    {
        hr = ConvertToI444(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_P010)
    {
        hr = ConvertToI420_10bit(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_P210)
    {
        hr = ConvertToI422_10bit(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_Y410)
    {
        hr = ConvertToI444_10bit(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_P016)
    {
        hr = ConvertToI420_12bit(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_P216)
    {
        hr = ConvertToI422_12bit(img, pDest);
    }
    else if (m_outputSubtype == MFVideoFormat_Y416)
    {
        hr = ConvertToI444_12bit(img, pDest);
    }
    else
    {
        hr = E_FAIL;
    }

    pBuffer->Unlock();

    if (FAILED(hr))
        return hr;

    pBuffer->SetCurrentLength(cbBuffer);

    hr = pSample->AddBuffer(pBuffer.Get());
    if (FAILED(hr))
        return hr;

    *ppSample = pSample.Detach();

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI420(const vpx_image_t* img, BYTE* pDest)
{
    // I420 is planar YUV 4:2:0
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    BYTE* pDestY = pDest;
    BYTE* pDestU = pDest + width * height;
    BYTE* pDestV = pDestU + (width * height / 4);

    // Copy Y plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestY + i * width,
            img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y],
            width);
    }

    // Copy U plane
    for (unsigned int i = 0; i < height / 2; i++)
    {
        memcpy(pDestU + i * (width / 2),
            img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U],
            width / 2);
    }

    // Copy V plane
    for (unsigned int i = 0; i < height / 2; i++)
    {
        memcpy(pDestV + i * (width / 2),
            img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V],
            width / 2);
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI422(const vpx_image_t* img, BYTE* pDest)
{
    // I422 is planar YUV 4:2:2
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    BYTE* pDestY = pDest;
    BYTE* pDestU = pDest + width * height;
    BYTE* pDestV = pDestU + (width * height / 2);

    // Copy Y plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestY + i * width,
            img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y],
            width);
    }

    // Copy U plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestU + i * (width / 2),
            img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U],
            width / 2);
    }

    // Copy V plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestV + i * (width / 2),
            img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V],
            width / 2);
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI444(const vpx_image_t* img, BYTE* pDest)
{
    // I444 is planar YUV 4:4:4
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    BYTE* pDestY = pDest;
    BYTE* pDestU = pDest + width * height;
    BYTE* pDestV = pDestU + width * height;

    // Copy Y plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestY + i * width,
            img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y],
            width);
    }

    // Copy U plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestU + i * width,
            img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U],
            width);
    }

    // Copy V plane
    for (unsigned int i = 0; i < height; i++)
    {
        memcpy(pDestV + i * width,
            img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V],
            width);
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI420_10bit(const vpx_image_t* img, BYTE* pDest)
{
    if (!img || !pDest)
    {
        return E_POINTER;
    }

    UINT width = img->d_w;
    UINT height = img->d_h;

    // P010格式固定为4:2:0，所以需要对4:2:2和4:4:4进行降采样
    UINT chromaWidth = (width + 1) / 2;
    UINT chromaHeight = (height + 1) / 2;

    // P010格式布局
    UINT16* pDestY = (UINT16*)pDest;
    UINT16* pDestUV = (UINT16*)(pDest + width * height * 2);

    // 确定源格式的色度采样比例和位深度
    bool is8bit = false;
    bool is422 = false;  // 水平1/2，垂直全分辨率
    bool is444 = false;  // 全分辨率
    UINT bitDepth = 8;

    switch (img->fmt)
    {
    case VPX_IMG_FMT_I420:
        is8bit = true;
        bitDepth = 8;
        break;
    case VPX_IMG_FMT_I42016:
        bitDepth = img->bit_depth > 0 ? img->bit_depth : 10;
        break;
    case VPX_IMG_FMT_I422:
        is8bit = true;
        is422 = true;
        bitDepth = 8;
        break;
    case VPX_IMG_FMT_I42216:
        is422 = true;
        bitDepth = img->bit_depth > 0 ? img->bit_depth : 10;
        break;
    case VPX_IMG_FMT_I444:
        is8bit = true;
        is444 = true;
        bitDepth = 8;
        break;
    case VPX_IMG_FMT_I44416:
        is444 = true;
        bitDepth = img->bit_depth > 0 ? img->bit_depth : 10;
        break;
    default:
        return E_INVALIDARG;
    }

    // ========== 处理Y平面 ==========
    if (is8bit)
    {
        // 8位Y平面
        BYTE* srcY = img->planes[VPX_PLANE_Y];
        for (UINT y = 0; y < height; y++)
        {
            for (UINT x = 0; x < width; x++)
            {
                // 8位 -> 10位 -> P010格式 (左移8位到MSB位置)
                pDestY[x] = (UINT16)srcY[x] << 8;
            }
            srcY += img->stride[VPX_PLANE_Y];
            pDestY += width;
        }
    }
    else
    {
        // 高位深Y平面
        UINT16* srcY = (UINT16*)img->planes[VPX_PLANE_Y];
        for (UINT y = 0; y < height; y++)
        {
            for (UINT x = 0; x < width; x++)
            {
                // 转换为10位并左移6位对齐到MSB
                if (bitDepth == 10)
                {
                    pDestY[x] = srcY[x] << 6;
                }
                else if (bitDepth > 10)
                {
                    // 12位或更高 -> 10位
                    pDestY[x] = (srcY[x] >> (bitDepth - 10)) << 6;
                }
                else
                {
                    // 小于10位 -> 10位
                    pDestY[x] = (srcY[x] << (10 - bitDepth)) << 6;
                }
            }
            srcY += img->stride[VPX_PLANE_Y] / 2;
            pDestY += width;
        }
    }

    // ========== 处理UV平面 ==========
    if (is444)
    {
        // 4:4:4 -> 4:2:0: 水平和垂直都需要2x降采样
        if (is8bit)
        {
            BYTE* srcU = img->planes[VPX_PLANE_U];
            BYTE* srcV = img->planes[VPX_PLANE_V];

            for (UINT y = 0; y < chromaHeight; y++)
            {
                UINT srcY = y * 2;
                BYTE* srcURow1 = srcU + srcY * img->stride[VPX_PLANE_U];
                BYTE* srcURow2 = srcU + (srcY + 1 < height ? srcY + 1 : srcY) * img->stride[VPX_PLANE_U];
                BYTE* srcVRow1 = srcV + srcY * img->stride[VPX_PLANE_V];
                BYTE* srcVRow2 = srcV + (srcY + 1 < height ? srcY + 1 : srcY) * img->stride[VPX_PLANE_V];

                for (UINT x = 0; x < chromaWidth; x++)
                {
                    UINT srcX = x * 2;
                    // 2x2区域平均
                    UINT u = (srcURow1[srcX] + srcURow1[srcX + 1 < width ? srcX + 1 : srcX] +
                        srcURow2[srcX] + srcURow2[srcX + 1 < width ? srcX + 1 : srcX] + 2) / 4;
                    UINT v = (srcVRow1[srcX] + srcVRow1[srcX + 1 < width ? srcX + 1 : srcX] +
                        srcVRow2[srcX] + srcVRow2[srcX + 1 < width ? srcX + 1 : srcX] + 2) / 4;

                    pDestUV[x * 2] = (UINT16)u << 8;
                    pDestUV[x * 2 + 1] = (UINT16)v << 8;
                }
                pDestUV += chromaWidth * 2;
            }
        }
        else
        {
            // 高位深4:4:4
            UINT16* srcU = (UINT16*)img->planes[VPX_PLANE_U];
            UINT16* srcV = (UINT16*)img->planes[VPX_PLANE_V];
            UINT strideU = img->stride[VPX_PLANE_U] / 2;
            UINT strideV = img->stride[VPX_PLANE_V] / 2;

            for (UINT y = 0; y < chromaHeight; y++)
            {
                UINT srcY = y * 2;
                UINT16* srcURow1 = srcU + srcY * strideU;
                UINT16* srcURow2 = srcU + (srcY + 1 < height ? srcY + 1 : srcY) * strideU;
                UINT16* srcVRow1 = srcV + srcY * strideV;
                UINT16* srcVRow2 = srcV + (srcY + 1 < height ? srcY + 1 : srcY) * strideV;

                for (UINT x = 0; x < chromaWidth; x++)
                {
                    UINT srcX = x * 2;
                    UINT u = (srcURow1[srcX] + srcURow1[srcX + 1 < width ? srcX + 1 : srcX] +
                        srcURow2[srcX] + srcURow2[srcX + 1 < width ? srcX + 1 : srcX] + 2) / 4;
                    UINT v = (srcVRow1[srcX] + srcVRow1[srcX + 1 < width ? srcX + 1 : srcX] +
                        srcVRow2[srcX] + srcVRow2[srcX + 1 < width ? srcX + 1 : srcX] + 2) / 4;

                    // 位深度转换
                    if (bitDepth == 10)
                    {
                        pDestUV[x * 2] = u << 6;
                        pDestUV[x * 2 + 1] = v << 6;
                    }
                    else if (bitDepth > 10)
                    {
                        pDestUV[x * 2] = (u >> (bitDepth - 10)) << 6;
                        pDestUV[x * 2 + 1] = (v >> (bitDepth - 10)) << 6;
                    }
                    else
                    {
                        pDestUV[x * 2] = (u << (10 - bitDepth)) << 6;
                        pDestUV[x * 2 + 1] = (v << (10 - bitDepth)) << 6;
                    }
                }
                pDestUV += chromaWidth * 2;
            }
        }
    }
    else if (is422)
    {
        // 4:2:2 -> 4:2:0: 只需要垂直降采样
        if (is8bit)
        {
            BYTE* srcU = img->planes[VPX_PLANE_U];
            BYTE* srcV = img->planes[VPX_PLANE_V];

            for (UINT y = 0; y < chromaHeight; y++)
            {
                UINT srcY = y * 2;
                BYTE* srcURow1 = srcU + srcY * img->stride[VPX_PLANE_U];
                BYTE* srcURow2 = srcU + (srcY + 1 < height ? srcY + 1 : srcY) * img->stride[VPX_PLANE_U];
                BYTE* srcVRow1 = srcV + srcY * img->stride[VPX_PLANE_V];
                BYTE* srcVRow2 = srcV + (srcY + 1 < height ? srcY + 1 : srcY) * img->stride[VPX_PLANE_V];

                for (UINT x = 0; x < chromaWidth; x++)
                {
                    // 垂直方向2个像素平均
                    UINT u = (srcURow1[x] + srcURow2[x] + 1) / 2;
                    UINT v = (srcVRow1[x] + srcVRow2[x] + 1) / 2;

                    pDestUV[x * 2] = (UINT16)u << 8;
                    pDestUV[x * 2 + 1] = (UINT16)v << 8;
                }
                pDestUV += chromaWidth * 2;
            }
        }
        else
        {
            // 高位深4:2:2
            UINT16* srcU = (UINT16*)img->planes[VPX_PLANE_U];
            UINT16* srcV = (UINT16*)img->planes[VPX_PLANE_V];
            UINT strideU = img->stride[VPX_PLANE_U] / 2;
            UINT strideV = img->stride[VPX_PLANE_V] / 2;

            for (UINT y = 0; y < chromaHeight; y++)
            {
                UINT srcY = y * 2;
                UINT16* srcURow1 = srcU + srcY * strideU;
                UINT16* srcURow2 = srcU + (srcY + 1 < height ? srcY + 1 : srcY) * strideU;
                UINT16* srcVRow1 = srcV + srcY * strideV;
                UINT16* srcVRow2 = srcV + (srcY + 1 < height ? srcY + 1 : srcY) * strideV;

                for (UINT x = 0; x < chromaWidth; x++)
                {
                    UINT u = (srcURow1[x] + srcURow2[x] + 1) / 2;
                    UINT v = (srcVRow1[x] + srcVRow2[x] + 1) / 2;

                    if (bitDepth == 10)
                    {
                        pDestUV[x * 2] = u << 6;
                        pDestUV[x * 2 + 1] = v << 6;
                    }
                    else if (bitDepth > 10)
                    {
                        pDestUV[x * 2] = (u >> (bitDepth - 10)) << 6;
                        pDestUV[x * 2 + 1] = (v >> (bitDepth - 10)) << 6;
                    }
                    else
                    {
                        pDestUV[x * 2] = (u << (10 - bitDepth)) << 6;
                        pDestUV[x * 2 + 1] = (v << (10 - bitDepth)) << 6;
                    }
                }
                pDestUV += chromaWidth * 2;
            }
        }
    }
    else
    {
        // 4:2:0 -> 4:2:0: 直接复制并转换位深度
        if (is8bit)
        {
            BYTE* srcU = img->planes[VPX_PLANE_U];
            BYTE* srcV = img->planes[VPX_PLANE_V];

            for (UINT y = 0; y < chromaHeight; y++)
            {
                for (UINT x = 0; x < chromaWidth; x++)
                {
                    pDestUV[x * 2] = (UINT16)srcU[x] << 8;
                    pDestUV[x * 2 + 1] = (UINT16)srcV[x] << 8;
                }
                srcU += img->stride[VPX_PLANE_U];
                srcV += img->stride[VPX_PLANE_V];
                pDestUV += chromaWidth * 2;
            }
        }
        else
        {
            // 高位深4:2:0
            UINT16* srcU = (UINT16*)img->planes[VPX_PLANE_U];
            UINT16* srcV = (UINT16*)img->planes[VPX_PLANE_V];

            for (UINT y = 0; y < chromaHeight; y++)
            {
                for (UINT x = 0; x < chromaWidth; x++)
                {
                    if (bitDepth == 10)
                    {
                        pDestUV[x * 2] = srcU[x] << 6;
                        pDestUV[x * 2 + 1] = srcV[x] << 6;
                    }
                    else if (bitDepth > 10)
                    {
                        pDestUV[x * 2] = (srcU[x] >> (bitDepth - 10)) << 6;
                        pDestUV[x * 2 + 1] = (srcV[x] >> (bitDepth - 10)) << 6;
                    }
                    else
                    {
                        pDestUV[x * 2] = (srcU[x] << (10 - bitDepth)) << 6;
                        pDestUV[x * 2 + 1] = (srcV[x] << (10 - bitDepth)) << 6;
                    }
                }
                srcU += img->stride[VPX_PLANE_U] / 2;
                srcV += img->stride[VPX_PLANE_V] / 2;
                pDestUV += chromaWidth * 2;
            }
        }
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI422_10bit(const vpx_image_t* img, BYTE* pDest)
{
    // P210 is 10-bit 4:2:2
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    UINT16* pDestY = (UINT16*)pDest;
    UINT16* pDestUV = pDestY + width * height;

    // Copy Y plane
    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcY = (UINT16*)(img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y]);
        for (unsigned int j = 0; j < width; j++)
        {
            pDestY[i * width + j] = pSrcY[j] << 6;
        }
    }

    // Interleave U and V planes
    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcU = (UINT16*)(img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U]);
        UINT16* pSrcV = (UINT16*)(img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V]);

        for (unsigned int j = 0; j < width / 2; j++)
        {
            pDestUV[i * width + j * 2] = pSrcU[j] << 6;
            pDestUV[i * width + j * 2 + 1] = pSrcV[j] << 6;
        }
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI444_10bit(const vpx_image_t* img, BYTE* pDest)
{
    // Y410 is 10-bit 4:4:4 packed format
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    UINT32* pDest32 = (UINT32*)pDest;

    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcY = (UINT16*)(img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y]);
        UINT16* pSrcU = (UINT16*)(img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U]);
        UINT16* pSrcV = (UINT16*)(img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V]);

        for (unsigned int j = 0; j < width; j++)
        {
            UINT32 U = pSrcU[j] & 0x3FF;
            UINT32 Y = pSrcY[j] & 0x3FF;
            UINT32 V = pSrcV[j] & 0x3FF;
            UINT32 A = 0x3; // 2-bit alpha, fully opaque

            pDest32[i * width + j] = (A << 30) | (V << 20) | (Y << 10) | U;
        }
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI420_12bit(const vpx_image_t* img, BYTE* pDest)
{
    // P016 is 12-bit 4:2:0 (stored as 16-bit)
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    UINT16* pDestY = (UINT16*)pDest;
    UINT16* pDestUV = pDestY + width * height;

    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcY = (UINT16*)(img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y]);
        for (unsigned int j = 0; j < width; j++)
        {
            pDestY[i * width + j] = pSrcY[j] << 4; // 12-bit to 16-bit
        }
    }

    for (unsigned int i = 0; i < height / 2; i++)
    {
        UINT16* pSrcU = (UINT16*)(img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U]);
        UINT16* pSrcV = (UINT16*)(img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V]);

        for (unsigned int j = 0; j < width / 2; j++)
        {
            pDestUV[i * width + j * 2] = pSrcU[j] << 4;
            pDestUV[i * width + j * 2 + 1] = pSrcV[j] << 4;
        }
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI422_12bit(const vpx_image_t* img, BYTE* pDest)
{
    // P216 is 12-bit 4:2:2
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    UINT16* pDestY = (UINT16*)pDest;
    UINT16* pDestUV = pDestY + width * height;

    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcY = (UINT16*)(img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y]);
        for (unsigned int j = 0; j < width; j++)
        {
            pDestY[i * width + j] = pSrcY[j] << 4;
        }
    }

    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcU = (UINT16*)(img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U]);
        UINT16* pSrcV = (UINT16*)(img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V]);

        for (unsigned int j = 0; j < width / 2; j++)
        {
            pDestUV[i * width + j * 2] = pSrcU[j] << 4;
            pDestUV[i * width + j * 2 + 1] = pSrcV[j] << 4;
        }
    }

    return S_OK;
}

HRESULT VP9Decoder::ConvertToI444_12bit(const vpx_image_t* img, BYTE* pDest)
{
    // Y416 is 16-bit 4:4:4 (can store 12-bit)
    UINT32 width = img->d_w;
    UINT32 height = img->d_h;

    UINT16* pDest16 = (UINT16*)pDest;

    for (unsigned int i = 0; i < height; i++)
    {
        UINT16* pSrcY = (UINT16*)(img->planes[VPX_PLANE_Y] + i * img->stride[VPX_PLANE_Y]);
        UINT16* pSrcU = (UINT16*)(img->planes[VPX_PLANE_U] + i * img->stride[VPX_PLANE_U]);
        UINT16* pSrcV = (UINT16*)(img->planes[VPX_PLANE_V] + i * img->stride[VPX_PLANE_V]);

        for (unsigned int j = 0; j < width; j++)
        {
            pDest16[(i * width + j) * 4 + 0] = pSrcU[j] << 4; // U
            pDest16[(i * width + j) * 4 + 1] = pSrcY[j] << 4; // Y
            pDest16[(i * width + j) * 4 + 2] = pSrcV[j] << 4; // V
            pDest16[(i * width + j) * 4 + 3] = 0xFFFF;        // A
        }
    }

    return S_OK;
}

HRESULT VP9Decoder::OnCheckInputType(IMFMediaType* pType)
{
    if (!IsValidInputType(pType))
        return MF_E_INVALIDMEDIATYPE;

    return S_OK;
}

HRESULT VP9Decoder::OnCheckOutputType(IMFMediaType* pType)
{
    if (!m_pInputType)
        return MF_E_TRANSFORM_TYPE_NOT_SET;

    if (!IsValidOutputType(pType))
        return MF_E_INVALIDMEDIATYPE;

    return S_OK;
}

HRESULT VP9Decoder::OnSetInputType(IMFMediaType* pType)
{
    m_pInputType = pType;

    UINT64 frameSize = 0;
    if (SUCCEEDED(pType->GetUINT64(MF_MT_FRAME_SIZE, &frameSize)))
    {
        m_uiWidth = (UINT32)(frameSize >> 32);
        m_uiHeight = (UINT32)(frameSize & 0xFFFFFFFF);
    }

    UINT64 frameRate = 0;
    if (SUCCEEDED(pType->GetUINT64(MF_MT_FRAME_RATE, &frameRate)))
    {
        m_uiFrameRateNum = (UINT32)(frameRate >> 32);
        m_uiFrameRateDen = (UINT32)(frameRate & 0xFFFFFFFF);
    }

    return S_OK;
}

HRESULT VP9Decoder::OnSetOutputType(IMFMediaType* pType)
{
    m_pOutputType = pType;
    pType->GetGUID(MF_MT_SUBTYPE, &m_outputSubtype);
    return S_OK;
}

HRESULT VP9Decoder::OnFlush()
{
    m_pPendingSample.Reset();
    return S_OK;
}

HRESULT VP9Decoder::OnDrain()
{
    return S_OK;
}

bool VP9Decoder::IsValidInputType(IMFMediaType* pType)
{
    if (!pType)
        return false;

    GUID majorType = { 0 };
    GUID subType = { 0 };

    if (FAILED(pType->GetGUID(MF_MT_MAJOR_TYPE, &majorType)))
        return false;

    if (FAILED(pType->GetGUID(MF_MT_SUBTYPE, &subType)))
        return false;

    if (majorType != MFMediaType_Video)
        return false;

    if (subType != MFVideoFormat_VP90)
        return false;

    return true;
}

bool VP9Decoder::IsValidOutputType(IMFMediaType* pType)
{
    if (!pType)
        return false;

    GUID majorType = { 0 };
    GUID subType = { 0 };

    if (FAILED(pType->GetGUID(MF_MT_MAJOR_TYPE, &majorType)))
        return false;

    if (FAILED(pType->GetGUID(MF_MT_SUBTYPE, &subType)))
        return false;

    if (majorType != MFMediaType_Video)
        return false;

    for (DWORD i = 0; i < g_NumOutputFormats; i++)
    {
        if (subType == g_OutputFormats[i])
            return true;
    }

    return false;
}

DWORD VP9Decoder::GetOutputBufferSize(const GUID& subtype, UINT32 width, UINT32 height)
{
    if (subtype == MFVideoFormat_IYUV)
    {
        return width * height * 3 / 2; // 8-bit 4:2:0
    }
    else if (subtype == MFVideoFormat_I422)
    {
        return width * height * 2; // 8-bit 4:2:2
    }
    else if (subtype == MFVideoFormat_I444)
    {
        return width * height * 3; // 8-bit 4:4:4
    }
    else if (subtype == MFVideoFormat_P010)
    {
        return width * height * 3; // 10-bit 4:2:0 (16-bit per component)
    }
    else if (subtype == MFVideoFormat_P210)
    {
        return width * height * 4; // 10-bit 4:2:2 (16-bit per component)
    }
    else if (subtype == MFVideoFormat_Y410)
    {
        return width * height * 4; // 10-bit 4:4:4 packed
    }
    else if (subtype == MFVideoFormat_P016)
    {
        return width * height * 3; // 12-bit 4:2:0 (16-bit per component)
    }
    else if (subtype == MFVideoFormat_P216)
    {
        return width * height * 4; // 12-bit 4:2:2 (16-bit per component)
    }
    else if (subtype == MFVideoFormat_Y416)
    {
        return width * height * 8; // 12-bit 4:4:4 (16-bit per component, UYVA)
    }

    return 0;
}