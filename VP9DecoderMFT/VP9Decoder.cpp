#include "pch.h"
#include "VP9Decoder.h"
#include <mfapi.h>
#include <thread>


static const GUID g_OutputFormats[] = {
	MFVideoFormat_NV12,   // 8-bit 4:2:0
	//MFVideoFormat_P010,   // 10-bit 4:2:0
	//MFVideoFormat_P016,   // 16-bit 4:2:0
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
	UINT threads = std::thread::hardware_concurrency();
	cfg.threads = threads > 0 ? threads : 8;

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
	if (cbBuffer == 0)
	{
		return E_FAIL;
	}

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
	if (m_outputSubtype == MFVideoFormat_NV12)
	{
		hr = ConvertToNV12(img, pDest);
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
	if (subtype == MFVideoFormat_NV12)
	{
		return width * height * 3 / 2; // 8-bit 4:2:0
	}
	else
	{
		return 0;
	}
}

HRESULT VP9Decoder::ConvertToNV12(const vpx_image_t* img, uint8_t* dst_buffer)
{
	if (!img || !dst_buffer)
		return E_POINTER;

	UINT32 width = img->d_w;
	UINT32 height = img->d_h;

	// NV12 布局
	uint8_t* dst_y = dst_buffer;                    // Y 平面
	uint8_t* dst_uv = dst_y + width * height;      // UV 平面（交错 U V U V ...）

	int dst_stride_y = width;
	int dst_stride_uv = width;  // UV 交错，每行有 width 个字节

	bool is_high_bit_depth = (img->fmt & VPX_IMG_FMT_HIGHBITDEPTH) != 0;

	if (!is_high_bit_depth)
	{
		// ========================================
		// 8-bit 输入 -> NV12
		// ========================================
		const uint8_t* src_y = img->planes[VPX_PLANE_Y];
		const uint8_t* src_u = img->planes[VPX_PLANE_U];
		const uint8_t* src_v = img->planes[VPX_PLANE_V];

		int src_stride_y = img->stride[VPX_PLANE_Y];
		int src_stride_u = img->stride[VPX_PLANE_U];
		int src_stride_v = img->stride[VPX_PLANE_V];

		int result = 0;

		if (img->fmt == VPX_IMG_FMT_I420)
		{
			// I420 -> NV12
			result = libyuv::I420ToNV12(
				src_y, src_stride_y,
				src_u, src_stride_u,
				src_v, src_stride_v,
				dst_y, dst_stride_y,
				dst_uv, dst_stride_uv,
				width, height
			);
		}
		else if (img->fmt == VPX_IMG_FMT_YV12)
		{
			// YV12 -> NV12 (V 和 U 交换)
			result = libyuv::I420ToNV12(
				src_y, src_stride_y,
				src_v, src_stride_v,  // V first in YV12
				src_u, src_stride_u,  // U second
				dst_y, dst_stride_y,
				dst_uv, dst_stride_uv,
				width, height
			);
		}
		else if (img->fmt == VPX_IMG_FMT_I422)
		{
			// I422 -> NV12
			// 复制 Y 平面
			libyuv::CopyPlane(
				src_y, src_stride_y,
				dst_y, dst_stride_y,
				width, height
			);

			// 垂直下采样并合并 U/V 到交错格式
			// 方法：先下采样到临时 I420，再转 NV12
			std::vector<uint8_t> temp_u((width / 2) * (height / 2));
			std::vector<uint8_t> temp_v((width / 2) * (height / 2));

			libyuv::ScalePlane(
				src_u, src_stride_u,
				width / 2, height,
				temp_u.data(), width / 2,
				width / 2, height / 2,
				libyuv::kFilterBilinear
			);

			libyuv::ScalePlane(
				src_v, src_stride_v,
				width / 2, height,
				temp_v.data(), width / 2,
				width / 2, height / 2,
				libyuv::kFilterBilinear
			);

			// 合并 U/V 为交错 UV
			libyuv::MergeUVPlane(
				temp_u.data(), width / 2,
				temp_v.data(), width / 2,
				dst_uv, dst_stride_uv,
				width / 2, height / 2
			);

			result = 0;
		}
		else if (img->fmt == VPX_IMG_FMT_I444 && img->cs != VPX_CS_SRGB)
		{
			// I444 -> NV12
			result = libyuv::I444ToNV12(
				src_y, src_stride_y,
				src_u, src_stride_u,
				src_v, src_stride_v,
				dst_y, dst_stride_y,
				dst_uv, dst_stride_uv,
				width, height
			);
		}
		else if (img->fmt == VPX_IMG_FMT_I444 && img->cs == VPX_CS_SRGB)
		{
			// GBR -> NV12
			std::vector<uint8_t> argb(width * height * 4);

			for (UINT32 y = 0; y < height; y++)
			{
				const uint8_t* src_g_row = src_y + y * src_stride_y;
				const uint8_t* src_b_row = src_u + y * src_stride_u;
				const uint8_t* src_r_row = src_v + y * src_stride_v;
				uint8_t* dst_row = argb.data() + y * width * 4;

				for (UINT32 x = 0; x < width; x++)
				{
					dst_row[x * 4 + 0] = src_b_row[x];  // B
					dst_row[x * 4 + 1] = src_g_row[x];  // G
					dst_row[x * 4 + 2] = src_r_row[x];  // R
					dst_row[x * 4 + 3] = 255;           // A
				}
			}

			result = libyuv::ARGBToNV12(
				argb.data(), width * 4,
				dst_y, dst_stride_y,
				dst_uv, dst_stride_uv,
				width, height);
		}
		else if (img->fmt == VPX_IMG_FMT_I440)
		{
			// I440 -> NV12
			// 复制 Y
			libyuv::CopyPlane(
				src_y, src_stride_y,
				dst_y, dst_stride_y,
				width, height
			);

			// 水平下采样 U/V
			std::vector<uint8_t> temp_u((width / 2) * (height / 2));
			std::vector<uint8_t> temp_v((width / 2) * (height / 2));

			libyuv::ScalePlane(
				src_u, src_stride_u,
				width, height / 2,
				temp_u.data(), width / 2,
				width / 2, height / 2,
				libyuv::kFilterBilinear
			);

			libyuv::ScalePlane(
				src_v, src_stride_v,
				width, height / 2,
				temp_v.data(), width / 2,
				width / 2, height / 2,
				libyuv::kFilterBilinear
			);

			// 合并 U/V
			libyuv::MergeUVPlane(
				temp_u.data(), width / 2,
				temp_v.data(), width / 2,
				dst_uv, dst_stride_uv,
				width / 2, height / 2
			);

			result = 0;
		}
		else if (img->fmt == VPX_IMG_FMT_NV12)
		{
			// NV12 -> NV12 (直接复制)
			// Y 平面
			libyuv::CopyPlane(
				src_y, src_stride_y,
				dst_y, dst_stride_y,
				width, height
			);

			// UV 平面（已经是交错的）
			const uint8_t* src_uv = src_u;  // NV12 中 U 平面指针实际是 UV
			int src_stride_uv = src_stride_u;

			libyuv::CopyPlane(
				src_uv, src_stride_uv,
				dst_uv, dst_stride_uv,
				width, height / 2
			);

			result = 0;
		}
		else
		{
			return E_NOTIMPL;
		}

		return (result == 0) ? S_OK : E_FAIL;
	}
	else
	{
		// ========================================
		// 高位深度输入 (10-bit/12-bit) -> 8-bit NV12
		// ========================================
		const uint16_t* src_y_16 = (const uint16_t*)img->planes[VPX_PLANE_Y];
		const uint16_t* src_u_16 = (const uint16_t*)img->planes[VPX_PLANE_U];
		const uint16_t* src_v_16 = (const uint16_t*)img->planes[VPX_PLANE_V];

		int src_stride_y = img->stride[VPX_PLANE_Y] / 2;
		int src_stride_u = img->stride[VPX_PLANE_U] / 2;
		int src_stride_v = img->stride[VPX_PLANE_V] / 2;

		int result = 0;

		if (img->bit_depth == 10)
		{
			// ===== 10-bit 输入 =====

			if (img->fmt == VPX_IMG_FMT_I42016)
			{
				// I010 (10-bit 4:2:0) -> NV12 (8-bit 4:2:0)
				// 先转为 I420(8-bit)，再转 NV12
				std::vector<uint8_t> temp_i420(width * height * 3 / 2);
				uint8_t* temp_y = temp_i420.data();
				uint8_t* temp_u = temp_y + width * height;
				uint8_t* temp_v = temp_u + (width / 2) * (height / 2);

				result = libyuv::I010ToI420(
					src_y_16, src_stride_y,
					src_u_16, src_stride_u,
					src_v_16, src_stride_v,
					temp_y, width,
					temp_u, width / 2,
					temp_v, width / 2,
					width, height
				);

				if (result == 0)
				{
					result = libyuv::I420ToNV12(
						temp_y, width,
						temp_u, width / 2,
						temp_v, width / 2,
						dst_y, dst_stride_y,
						dst_uv, dst_stride_uv,
						width, height
					);
				}
			}
			else if (img->fmt == VPX_IMG_FMT_I42216)
			{
				// I210 (10-bit 4:2:2) -> NV12 (8-bit 4:2:0)
				std::vector<uint8_t> temp_i420(width * height * 3 / 2);
				uint8_t* temp_y = temp_i420.data();
				uint8_t* temp_u = temp_y + width * height;
				uint8_t* temp_v = temp_u + (width / 2) * (height / 2);

				result = libyuv::I210ToI420(
					src_y_16, src_stride_y,
					src_u_16, src_stride_u,
					src_v_16, src_stride_v,
					temp_y, width,
					temp_u, width / 2,
					temp_v, width / 2,
					width, height
				);

				if (result == 0)
				{
					result = libyuv::I420ToNV12(
						temp_y, width,
						temp_u, width / 2,
						temp_v, width / 2,
						dst_y, dst_stride_y,
						dst_uv, dst_stride_uv,
						width, height
					);
				}
			}
			else if (img->fmt == VPX_IMG_FMT_I44416)
			{
				// I410 (10-bit 4:4:4) -> NV12 (8-bit 4:2:0)
				std::vector<uint8_t> temp_i420(width * height * 3 / 2);
				uint8_t* temp_y = temp_i420.data();
				uint8_t* temp_u = temp_y + width * height;
				uint8_t* temp_v = temp_u + (width / 2) * (height / 2);

				result = libyuv::I410ToI420(
					src_y_16, src_stride_y,
					src_u_16, src_stride_u,
					src_v_16, src_stride_v,
					temp_y, width,
					temp_u, width / 2,
					temp_v, width / 2,
					width, height
				);

				if (result == 0)
				{
					result = libyuv::I420ToNV12(
						temp_y, width,
						temp_u, width / 2,
						temp_v, width / 2,
						dst_y, dst_stride_y,
						dst_uv, dst_stride_uv,
						width, height
					);
				}
			}
			else if (img->fmt == VPX_IMG_FMT_I44016)
			{
				// I440(10-bit) -> NV12(8-bit)
				std::vector<uint8_t> temp_i420(width * height * 3 / 2);
				uint8_t* temp_y = temp_i420.data();
				uint8_t* temp_u = temp_y + width * height;
				uint8_t* temp_v = temp_u + (width / 2) * (height / 2);

				// 转换 Y
				int scale = 1 << 2;  // 10-bit -> 8-bit
				libyuv::Convert16To8Plane(
					src_y_16, src_stride_y,
					temp_y, width,
					scale,
					width, height
				);

				// 转换并水平下采样 U/V
				std::vector<uint8_t> temp_u_440(width * (height / 2));
				std::vector<uint8_t> temp_v_440(width * (height / 2));

				libyuv::Convert16To8Plane(
					src_u_16, src_stride_u,
					temp_u_440.data(), width,
					scale,
					width, height / 2
				);

				libyuv::Convert16To8Plane(
					src_v_16, src_stride_v,
					temp_v_440.data(), width,
					scale,
					width, height / 2
				);

				// 水平下采样
				libyuv::ScalePlane(
					temp_u_440.data(), width,
					width, height / 2,
					temp_u, width / 2,
					width / 2, height / 2,
					libyuv::kFilterBilinear
				);

				libyuv::ScalePlane(
					temp_v_440.data(), width,
					width, height / 2,
					temp_v, width / 2,
					width / 2, height / 2,
					libyuv::kFilterBilinear
				);

				// I420 -> NV12
				result = libyuv::I420ToNV12(
					temp_y, width,
					temp_u, width / 2,
					temp_v, width / 2,
					dst_y, dst_stride_y,
					dst_uv, dst_stride_uv,
					width, height
				);
			}
			else
			{
				return E_NOTIMPL;
			}
		}
		else if (img->bit_depth == 12)
		{
			// ===== 12-bit 输入 - 手动转换 =====

			// 创建临时 I420(8-bit) 缓冲区
			std::vector<uint8_t> temp_i420(width * height * 3 / 2);
			uint8_t* temp_y = temp_i420.data();
			uint8_t* temp_u = temp_y + width * height;
			uint8_t* temp_v = temp_u + (width / 2) * (height / 2);

			// 手动转换 Y 平面（12-bit -> 8-bit）
			for (UINT32 y = 0; y < height; y++)
			{
				const uint16_t* src_row = src_y_16 + y * src_stride_y;
				uint8_t* dst_row = temp_y + y * width;

				for (UINT32 x = 0; x < width; x++)
				{
					// 方法1: 简单右移（快速但可能损失精度）
					dst_row[x] = (uint8_t)(src_row[x] >> 4);

					// 方法2: 精确转换（更准确的色彩）
					// dst_row[x] = (uint8_t)((src_row[x] * 255 + 2047) / 4095);
				}
			}

			if (img->fmt == VPX_IMG_FMT_I42016)
			{
				// I012 (12-bit 4:2:0) -> NV12
				// 手动转换 U 平面
				for (UINT32 y = 0; y < height / 2; y++)
				{
					const uint16_t* src_u_row = src_u_16 + y * src_stride_u;
					const uint16_t* src_v_row = src_v_16 + y * src_stride_v;
					uint8_t* dst_u_row = temp_u + y * (width / 2);
					uint8_t* dst_v_row = temp_v + y * (width / 2);

					for (UINT32 x = 0; x < width / 2; x++)
					{
						dst_u_row[x] = (uint8_t)(src_u_row[x] >> 4);
						dst_v_row[x] = (uint8_t)(src_v_row[x] >> 4);
					}
				}
			}
			else if (img->fmt == VPX_IMG_FMT_I42216)
			{
				// I212 (12-bit 4:2:2) -> NV12
				// 转换并垂直下采样
				for (UINT32 y = 0; y < height / 2; y++)
				{
					const uint16_t* src_u_row0 = src_u_16 + (y * 2) * src_stride_u;
					const uint16_t* src_u_row1 = src_u_16 + (y * 2 + 1) * src_stride_u;
					const uint16_t* src_v_row0 = src_v_16 + (y * 2) * src_stride_v;
					const uint16_t* src_v_row1 = src_v_16 + (y * 2 + 1) * src_stride_v;
					uint8_t* dst_u_row = temp_u + y * (width / 2);
					uint8_t* dst_v_row = temp_v + y * (width / 2);

					for (UINT32 x = 0; x < width / 2; x++)
					{
						// 先平均，再转 8-bit
						uint32_t u_avg = (src_u_row0[x] + src_u_row1[x] + 1) >> 1;
						uint32_t v_avg = (src_v_row0[x] + src_v_row1[x] + 1) >> 1;

						dst_u_row[x] = (uint8_t)(u_avg >> 4);
						dst_v_row[x] = (uint8_t)(v_avg >> 4);
					}
				}
			}
			else if (img->fmt == VPX_IMG_FMT_I44416)
			{
				// I412 (12-bit 4:4:4) -> NV12
				// 转换并 2x2 下采样
				for (UINT32 y = 0; y < height / 2; y++)
				{
					const uint16_t* src_u_row0 = src_u_16 + (y * 2) * src_stride_u;
					const uint16_t* src_u_row1 = src_u_16 + (y * 2 + 1) * src_stride_u;
					const uint16_t* src_v_row0 = src_v_16 + (y * 2) * src_stride_v;
					const uint16_t* src_v_row1 = src_v_16 + (y * 2 + 1) * src_stride_v;
					uint8_t* dst_u_row = temp_u + y * (width / 2);
					uint8_t* dst_v_row = temp_v + y * (width / 2);

					for (UINT32 x = 0; x < width / 2; x++)
					{
						// 2x2 平均
						uint32_t u_sum = src_u_row0[x * 2] + src_u_row0[x * 2 + 1] +
							src_u_row1[x * 2] + src_u_row1[x * 2 + 1];
						uint32_t v_sum = src_v_row0[x * 2] + src_v_row0[x * 2 + 1] +
							src_v_row1[x * 2] + src_v_row1[x * 2 + 1];

						uint32_t u_avg = (u_sum + 2) >> 2;
						uint32_t v_avg = (v_sum + 2) >> 2;

						dst_u_row[x] = (uint8_t)(u_avg >> 4);
						dst_v_row[x] = (uint8_t)(v_avg >> 4);
					}
				}
			}
			else if (img->fmt == VPX_IMG_FMT_I44016)
			{
				// I412 (12-bit 4:4:0) -> NV12
				// 转换并水平下采样
				for (UINT32 y = 0; y < height / 2; y++)
				{
					const uint16_t* src_u_row = src_u_16 + y * src_stride_u;
					const uint16_t* src_v_row = src_v_16 + y * src_stride_v;
					uint8_t* dst_u_row = temp_u + y * (width / 2);
					uint8_t* dst_v_row = temp_v + y * (width / 2);

					for (UINT32 x = 0; x < width / 2; x++)
					{
						// 水平平均
						uint32_t u_avg = (src_u_row[x * 2] + src_u_row[x * 2 + 1] + 1) >> 1;
						uint32_t v_avg = (src_v_row[x * 2] + src_v_row[x * 2 + 1] + 1) >> 1;

						dst_u_row[x] = (uint8_t)(u_avg >> 4);
						dst_v_row[x] = (uint8_t)(v_avg >> 4);
					}
				}
			}
			else
			{
				return E_NOTIMPL;
			}

			// I420(8) -> NV12
			result = libyuv::I420ToNV12(
				temp_y, width,
				temp_u, width / 2,
				temp_v, width / 2,
				dst_y, dst_stride_y,
				dst_uv, dst_stride_uv,
				width, height
			);
		}
		else
		{
			return E_NOTIMPL;
		}

		return (result == 0) ? S_OK : E_FAIL;
	}
}
