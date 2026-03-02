#include "pch.h"
#include "VorbisDecoder.h"
#include <mfapi.h>


// 输出格式列表（按优先级排序）
static const GUID g_VorbisOutputFormats[] =
{
    MFAudioFormat_Float,   // 32-bit float（libvorbis 原生格式，首选）
    MFAudioFormat_PCM,     // 16-bit PCM
};
static const DWORD g_NumVorbisOutputFormats = ARRAYSIZE(g_VorbisOutputFormats);


// ============================================================
// 构造 / 析构
// ============================================================

VorbisDecoder::VorbisDecoder()
    : m_nRefCount(1)
    , m_bDecoderInitialized(false)
    , m_bVorbisInfoInited(false)
    , m_bStreamingStarted(false)
    , m_nHeaderPacketsReceived(0)
    , m_uiChannels(0)
    , m_uiSampleRate(0)
    , m_outputSubtype(GUID_NULL)
{
    ZeroMemory(&m_vorbisInfo, sizeof(m_vorbisInfo));
    ZeroMemory(&m_vorbisComment, sizeof(m_vorbisComment));
    ZeroMemory(&m_vorbisDspState, sizeof(m_vorbisDspState));
    ZeroMemory(&m_vorbisBlock, sizeof(m_vorbisBlock));

    MFCreateAttributes(&m_pAttributes, 3);
}

VorbisDecoder::~VorbisDecoder()
{
    ShutdownDecoder();
}


// ============================================================
// IUnknown
// ============================================================

STDMETHODIMP VorbisDecoder::QueryInterface(REFIID riid, void** ppv)
{
    if (!ppv)
        return E_POINTER;

    if (riid == IID_IUnknown)
        *ppv = static_cast<IUnknown*>(this);
    else if (riid == IID_IMFTransform)
        *ppv = static_cast<IMFTransform*>(this);
    else
    {
        *ppv = nullptr;
        return E_NOINTERFACE;
    }
    AddRef();
    return S_OK;
}

STDMETHODIMP_(ULONG) VorbisDecoder::AddRef()
{
    return InterlockedIncrement(&m_nRefCount);
}

STDMETHODIMP_(ULONG) VorbisDecoder::Release()
{
    ULONG count = InterlockedDecrement(&m_nRefCount);
    if (count == 0)
        delete this;
    return count;
}


// ============================================================
// IMFTransform — 流配置
// ============================================================

STDMETHODIMP VorbisDecoder::GetStreamLimits(
    DWORD* pdwInputMinimum, DWORD* pdwInputMaximum,
    DWORD* pdwOutputMinimum, DWORD* pdwOutputMaximum)
{
    if (!pdwInputMinimum || !pdwInputMaximum ||
        !pdwOutputMinimum || !pdwOutputMaximum)
        return E_POINTER;

    *pdwInputMinimum = *pdwInputMaximum = 1;
    *pdwOutputMinimum = *pdwOutputMaximum = 1;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetStreamCount(
    DWORD* pcInputStreams, DWORD* pcOutputStreams)
{
    if (!pcInputStreams || !pcOutputStreams)
        return E_POINTER;

    *pcInputStreams = 1;
    *pcOutputStreams = 1;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetStreamIDs(
    DWORD /*dwInputIDArraySize*/,  DWORD* /*pdwInputIDs*/,
    DWORD /*dwOutputIDArraySize*/, DWORD* /*pdwOutputIDs*/)
{
    // 固定 1 输入 1 输出，流 ID 始终为 0，不需要显式列举
    return E_NOTIMPL;
}

STDMETHODIMP VorbisDecoder::GetInputStreamInfo(
    DWORD dwInputStreamID, MFT_INPUT_STREAM_INFO* pStreamInfo)
{
    if (dwInputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (!pStreamInfo)         return E_POINTER;

    pStreamInfo->dwFlags =
        MFT_INPUT_STREAM_WHOLE_SAMPLES |
        MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER;
    pStreamInfo->cbSize         = 0;
    pStreamInfo->cbMaxLookahead = 0;
    pStreamInfo->cbAlignment    = 0;
    pStreamInfo->hnsMaxLatency  = 0;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetOutputStreamInfo(
    DWORD dwOutputStreamID, MFT_OUTPUT_STREAM_INFO* pStreamInfo)
{
    if (dwOutputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (!pStreamInfo)          return E_POINTER;

    // MFT 自行分配输出 sample；输出大小可变（取决于 Vorbis 块大小）
    pStreamInfo->dwFlags =
        MFT_OUTPUT_STREAM_WHOLE_SAMPLES |
        MFT_OUTPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER |
        MFT_OUTPUT_STREAM_PROVIDES_SAMPLES;
    pStreamInfo->cbSize      = 0;
    pStreamInfo->cbAlignment = 0;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetAttributes(IMFAttributes** ppAttributes)
{
    if (!ppAttributes) return E_POINTER;

    *ppAttributes = m_pAttributes.Get();
    if (*ppAttributes)
        (*ppAttributes)->AddRef();
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetInputStreamAttributes(
    DWORD /*dwInputStreamID*/, IMFAttributes** /*ppAttributes*/)
{
    return E_NOTIMPL;
}

STDMETHODIMP VorbisDecoder::GetOutputStreamAttributes(
    DWORD /*dwOutputStreamID*/, IMFAttributes** /*ppAttributes*/)
{
    return E_NOTIMPL;
}

STDMETHODIMP VorbisDecoder::DeleteInputStream(DWORD /*dwStreamID*/)
{
    return E_NOTIMPL;
}

STDMETHODIMP VorbisDecoder::AddInputStreams(
    DWORD /*cStreams*/, DWORD* /*adwStreamIDs*/)
{
    return E_NOTIMPL;
}


// ============================================================
// IMFTransform — 媒体类型
// ============================================================

STDMETHODIMP VorbisDecoder::GetInputAvailableType(
    DWORD dwInputStreamID, DWORD dwTypeIndex, IMFMediaType** ppType)
{
    if (dwInputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (!ppType)              return E_POINTER;
    if (dwTypeIndex != 0)     return MF_E_NO_MORE_TYPES;

    HRESULT hr;
    ComPtr<IMFMediaType> pType;

    hr = MFCreateMediaType(&pType);
    if (FAILED(hr)) return hr;

    hr = pType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);
    if (FAILED(hr)) return hr;

    hr = pType->SetGUID(MF_MT_SUBTYPE, MFAudioFormat_Vorbis);
    if (FAILED(hr)) return hr;

    *ppType = pType.Detach();
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetOutputAvailableType(
    DWORD dwOutputStreamID, DWORD dwTypeIndex, IMFMediaType** ppType)
{
    if (dwOutputStreamID != 0)            return MF_E_INVALIDSTREAMNUMBER;
    if (!ppType)                          return E_POINTER;
    if (!m_pInputType)                    return MF_E_TRANSFORM_TYPE_NOT_SET;
    if (dwTypeIndex >= g_NumVorbisOutputFormats) return MF_E_NO_MORE_TYPES;

    HRESULT hr;
    ComPtr<IMFMediaType> pType;

    hr = MFCreateMediaType(&pType);
    if (FAILED(hr)) return hr;

    const GUID& subtype = g_VorbisOutputFormats[dwTypeIndex];
    UINT32 bitsPerSample = (subtype == MFAudioFormat_Float) ? 32 : 16;

    hr = pType->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Audio);
    if (FAILED(hr)) return hr;

    hr = pType->SetGUID(MF_MT_SUBTYPE, subtype);
    if (FAILED(hr)) return hr;

    if (m_uiChannels > 0 && m_uiSampleRate > 0)
    {
        UINT32 blockAlign = m_uiChannels * (bitsPerSample / 8);
        UINT32 avgBytesPerSec = blockAlign * m_uiSampleRate;

        hr = pType->SetUINT32(MF_MT_AUDIO_NUM_CHANNELS, m_uiChannels);
        if (FAILED(hr)) return hr;

        hr = pType->SetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND, m_uiSampleRate);
        if (FAILED(hr)) return hr;

        hr = pType->SetUINT32(MF_MT_AUDIO_BITS_PER_SAMPLE, bitsPerSample);
        if (FAILED(hr)) return hr;

        hr = pType->SetUINT32(MF_MT_AUDIO_BLOCK_ALIGNMENT, blockAlign);
        if (FAILED(hr)) return hr;

        hr = pType->SetUINT32(MF_MT_AUDIO_AVG_BYTES_PER_SECOND, avgBytesPerSec);
        if (FAILED(hr)) return hr;

        hr = pType->SetUINT32(MF_MT_ALL_SAMPLES_INDEPENDENT, TRUE);
        if (FAILED(hr)) return hr;
    }

    *ppType = pType.Detach();
    return S_OK;
}

STDMETHODIMP VorbisDecoder::SetInputType(
    DWORD dwInputStreamID, IMFMediaType* pType, DWORD dwFlags)
{
    if (dwInputStreamID != 0)           return MF_E_INVALIDSTREAMNUMBER;
    if (dwFlags & ~MFT_SET_TYPE_TEST_ONLY) return E_INVALIDARG;

    HRESULT hr = S_OK;

    if (m_pPendingSample)
        return MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;

    if (pType)
    {
        hr = OnCheckInputType(pType);
        if (FAILED(hr)) return hr;
    }

    if (!(dwFlags & MFT_SET_TYPE_TEST_ONLY))
    {
        if (pType)
            hr = OnSetInputType(pType);
        else
        {
            m_pInputType.Reset();
            ShutdownDecoder();
        }
    }

    return hr;
}

STDMETHODIMP VorbisDecoder::SetOutputType(
    DWORD dwOutputStreamID, IMFMediaType* pType, DWORD dwFlags)
{
    if (dwOutputStreamID != 0)          return MF_E_INVALIDSTREAMNUMBER;
    if (dwFlags & ~MFT_SET_TYPE_TEST_ONLY) return E_INVALIDARG;

    HRESULT hr = S_OK;

    if (m_pPendingSample)
        return MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;

    if (pType)
    {
        hr = OnCheckOutputType(pType);
        if (FAILED(hr)) return hr;
    }

    if (!(dwFlags & MFT_SET_TYPE_TEST_ONLY))
    {
        if (pType)
            hr = OnSetOutputType(pType);
        else
        {
            m_pOutputType.Reset();
            m_outputSubtype = GUID_NULL;
        }
    }

    return hr;
}

STDMETHODIMP VorbisDecoder::GetInputCurrentType(
    DWORD dwInputStreamID, IMFMediaType** ppType)
{
    if (dwInputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (!ppType)              return E_POINTER;
    if (!m_pInputType)        return MF_E_TRANSFORM_TYPE_NOT_SET;

    *ppType = m_pInputType.Get();
    (*ppType)->AddRef();
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetOutputCurrentType(
    DWORD dwOutputStreamID, IMFMediaType** ppType)
{
    if (dwOutputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (!ppType)               return E_POINTER;
    if (!m_pOutputType)        return MF_E_TRANSFORM_TYPE_NOT_SET;

    *ppType = m_pOutputType.Get();
    (*ppType)->AddRef();
    return S_OK;
}


// ============================================================
// IMFTransform — 状态查询
// ============================================================

STDMETHODIMP VorbisDecoder::GetInputStatus(
    DWORD dwInputStreamID, DWORD* pdwFlags)
{
    if (dwInputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (!pdwFlags)            return E_POINTER;

    *pdwFlags = m_pPendingSample ? 0 : MFT_INPUT_STATUS_ACCEPT_DATA;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::GetOutputStatus(DWORD* pdwFlags)
{
    if (!pdwFlags) return E_POINTER;

    *pdwFlags = m_pPendingSample ? MFT_OUTPUT_STATUS_SAMPLE_READY : 0;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::SetOutputBounds(
    LONGLONG /*hnsLowerBound*/, LONGLONG /*hnsUpperBound*/)
{
    return E_NOTIMPL;
}

STDMETHODIMP VorbisDecoder::ProcessEvent(
    DWORD /*dwInputStreamID*/, IMFMediaEvent* /*pEvent*/)
{
    return E_NOTIMPL;
}


// ============================================================
// IMFTransform — 消息处理
// ============================================================

STDMETHODIMP VorbisDecoder::ProcessMessage(
    MFT_MESSAGE_TYPE eMessage, ULONG_PTR /*ulParam*/)
{
    switch (eMessage)
    {
    case MFT_MESSAGE_COMMAND_FLUSH:
        return OnFlush();

    case MFT_MESSAGE_COMMAND_DRAIN:
        return OnDrain();

    case MFT_MESSAGE_NOTIFY_BEGIN_STREAMING:
        m_bStreamingStarted = true;
        return S_OK;

    case MFT_MESSAGE_NOTIFY_END_STREAMING:
        m_bStreamingStarted = false;
        return S_OK;

    case MFT_MESSAGE_NOTIFY_START_OF_STREAM:
    case MFT_MESSAGE_NOTIFY_END_OF_STREAM:
    case MFT_MESSAGE_SET_D3D_MANAGER:
        return S_OK;

    default:
        return S_OK;
    }
}


// ============================================================
// IMFTransform — 样本处理
// ============================================================

STDMETHODIMP VorbisDecoder::ProcessInput(
    DWORD dwInputStreamID, IMFSample* pSample, DWORD dwFlags)
{
    if (dwInputStreamID != 0) return MF_E_INVALIDSTREAMNUMBER;
    if (dwFlags != 0)         return E_INVALIDARG;
    if (!pSample)             return E_POINTER;

    if (!m_pInputType || !m_pOutputType)
        return MF_E_NOTACCEPTING;

    if (m_pPendingSample)
        return MF_E_NOTACCEPTING;

    // 注意：即使解码器尚未初始化也接受数据包。
    // 部分 MF 管线将前 3 个 Vorbis header packet 作为普通 ProcessInput 数据发送，
    // DecodePacket 内部会识别并处理这些 header 包以完成自初始化。

    m_pPendingSample = pSample;
    return S_OK;
}

STDMETHODIMP VorbisDecoder::ProcessOutput(
    DWORD dwFlags,
    DWORD cOutputBufferCount,
    MFT_OUTPUT_DATA_BUFFER* pOutputSamples,
    DWORD* pdwStatus)
{
    if (dwFlags != 0)                                    return E_INVALIDARG;
    if (cOutputBufferCount != 1 || !pOutputSamples || !pdwStatus) return E_INVALIDARG;

    if (!m_pInputType || !m_pOutputType)
        return MF_E_TRANSFORM_TYPE_NOT_SET;

    if (!m_pPendingSample)
        return MF_E_TRANSFORM_NEED_MORE_INPUT;

    pOutputSamples[0].dwStatus = 0;
    pOutputSamples[0].pEvents  = nullptr;
    pOutputSamples[0].pSample  = nullptr;

    // 先将 pending sample 移出，无论解码结果如何该包都算已消费
    // 这样管线在收到 MF_E_TRANSFORM_NEED_MORE_INPUT 后，
    // 下一次 ProcessInput 不会因 m_pPendingSample 非空而返回 MF_E_NOTACCEPTING
    ComPtr<IMFSample> pPending;
    pPending.Swap(m_pPendingSample); // m_pPendingSample 变为 null

    HRESULT hr = DecodePacket(pPending.Get(), &pOutputSamples[0].pSample);

    *pdwStatus = 0;
    return hr;
}


// ============================================================
// 私有辅助方法
// ============================================================

// 解析 XiphLacing 格式，提取 Vorbis 的 3 个 header packet
HRESULT VorbisDecoder::ParseXiphLacingHeaders(
    const BYTE* pData, DWORD cbData,
    std::vector<std::vector<BYTE>>& headers)
{
    if (!pData || cbData < 3)
        return E_INVALIDARG;

    const BYTE* p   = pData;
    const BYTE* end = pData + cbData;

    // 第一个字节：包数量 - 1（Vorbis 固定为 0x02，表示 3 个包）
    BYTE numPacketsMinus1 = *p++;
    DWORD numPackets = (DWORD)numPacketsMinus1 + 1;

    if (numPackets < 2)
        return E_INVALIDARG;

    // 解析前 (numPackets - 1) 个包的大小（XiphLacing 编码）
    std::vector<DWORD> sizes;
    DWORD totalSized = 0;

    for (DWORD i = 0; i < numPackets - 1; i++)
    {
        DWORD size = 0;
        BYTE  b    = 0;
        do
        {
            if (p >= end) return E_INVALIDARG;
            b = *p++;
            size += b;
        } while (b == 255);

        sizes.push_back(size);
        totalSized += size;
    }

    // 最后一个包的大小由剩余字节数确定
    DWORD consumed = (DWORD)(p - pData);
    if (consumed > cbData) return E_INVALIDARG;

    DWORD remaining = cbData - consumed;
    if (remaining < totalSized) return E_INVALIDARG;

    sizes.push_back(remaining - totalSized);

    // 提取各包数据
    headers.clear();
    for (DWORD i = 0; i < numPackets; i++)
    {
        DWORD sz = sizes[i];
        if (p + sz > end) return E_INVALIDARG;

        headers.emplace_back(p, p + sz);
        p += sz;
    }

    return S_OK;
}

// 用 MF_MT_USER_DATA 中的 3 个 Vorbis header 初始化 libvorbis 解码器
HRESULT VorbisDecoder::InitializeDecoder(
    const BYTE* pPrivateData, DWORD cbPrivateData)
{
    if (m_bDecoderInitialized)
        return S_OK;

    if (!pPrivateData || cbPrivateData == 0)
        return E_INVALIDARG;

    vorbis_info_init(&m_vorbisInfo);
    vorbis_comment_init(&m_vorbisComment);
    m_bVorbisInfoInited = true;

    // 解析 3 个 Vorbis 头包
    std::vector<std::vector<BYTE>> headerPackets;
    HRESULT hr = ParseXiphLacingHeaders(pPrivateData, cbPrivateData, headerPackets);
    if (FAILED(hr) || headerPackets.size() < 3)
    {
        vorbis_comment_clear(&m_vorbisComment);
        vorbis_info_clear(&m_vorbisInfo);
        m_bVorbisInfoInited = false;
        return FAILED(hr) ? hr : E_INVALIDARG;
    }

    // 依次提交 3 个头包给 libvorbis
    for (int i = 0; i < 3; i++)
    {
        ogg_packet op{};
        op.packet  = headerPackets[i].data();
        op.bytes   = (long)headerPackets[i].size();
        op.b_o_s   = (i == 0) ? 1 : 0;  // 第一个包是 BOS
        op.e_o_s   = 0;
        op.granulepos  = 0;
        op.packetno    = i;

        int ret = vorbis_synthesis_headerin(&m_vorbisInfo, &m_vorbisComment, &op);
        if (ret != 0)
        {
            vorbis_comment_clear(&m_vorbisComment);
            vorbis_info_clear(&m_vorbisInfo);
            m_bVorbisInfoInited = false;
            return E_FAIL;
        }
    }

    // 初始化 DSP 状态和块
    if (vorbis_synthesis_init(&m_vorbisDspState, &m_vorbisInfo) != 0)
    {
        vorbis_comment_clear(&m_vorbisComment);
        vorbis_info_clear(&m_vorbisInfo);
        m_bVorbisInfoInited = false;
        return E_FAIL;
    }

    if (vorbis_block_init(&m_vorbisDspState, &m_vorbisBlock) != 0)
    {
        vorbis_dsp_clear(&m_vorbisDspState);
        vorbis_comment_clear(&m_vorbisComment);
        vorbis_info_clear(&m_vorbisInfo);
        m_bVorbisInfoInited = false;
        return E_FAIL;
    }

    // 用 libvorbis 反馈的实际参数更新成员变量
    m_uiChannels            = (UINT32)m_vorbisInfo.channels;
    m_uiSampleRate          = (UINT32)m_vorbisInfo.rate;
    m_nHeaderPacketsReceived = 3; // 标记 header 已全部接收
    m_bDecoderInitialized   = true;
    return S_OK;
}

HRESULT VorbisDecoder::ShutdownDecoder()
{
    if (m_bDecoderInitialized)
    {
        vorbis_block_clear(&m_vorbisBlock);
        vorbis_dsp_clear(&m_vorbisDspState);
        vorbis_comment_clear(&m_vorbisComment);
        vorbis_info_clear(&m_vorbisInfo);
        m_bDecoderInitialized = false;
        m_bVorbisInfoInited   = false;
    }
    else if (m_bVorbisInfoInited)
    {
        // vorbis_info_init / vorbis_comment_init 已调用但未完成全量初始化，需要清理
        vorbis_comment_clear(&m_vorbisComment);
        vorbis_info_clear(&m_vorbisInfo);
        m_bVorbisInfoInited = false;
    }
    m_nHeaderPacketsReceived = 0;
    return S_OK;
}

// 判断原始字节流是否是 Vorbis header packet
// Vorbis header: 第一个字节为奇数（1=ident, 3=comment, 5=setup），后跟 "vorbis" magic
static bool IsVorbisHeaderPacket(const BYTE* pData, DWORD cbData)
{
    return cbData >= 7 &&
           (pData[0] == 1 || pData[0] == 3 || pData[0] == 5) &&
           memcmp(pData + 1, "vorbis", 6) == 0;
}

// 解码一个 Vorbis 数据包，返回一个 MF 音频 Sample
HRESULT VorbisDecoder::DecodePacket(
    IMFSample* pInputSample, IMFSample** ppOutputSample)
{
    if (!pInputSample || !ppOutputSample)
        return E_POINTER;

    HRESULT hr;
    ComPtr<IMFMediaBuffer> pBuffer;
    BYTE* pData  = nullptr;
    DWORD cbData = 0;

    hr = pInputSample->ConvertToContiguousBuffer(&pBuffer);
    if (FAILED(hr)) return hr;

    hr = pBuffer->Lock(&pData, nullptr, &cbData);
    if (FAILED(hr)) return hr;

    // -------------------------------------------------------
    // 内联初始化回退：若解码器尚未就绪，尝试从 header packet 自初始化
    // 这处理了部分 MF 管线不通过 MF_MT_USER_DATA 传递 headers、
    // 而是将前 3 个 header 包作为普通 ProcessInput 数据发送的场景
    // -------------------------------------------------------
    if (!m_bDecoderInitialized)
    {
        if (IsVorbisHeaderPacket(pData, cbData) && m_nHeaderPacketsReceived < 3)
        {
            // 第一个 header 包时初始化 vorbis_info / vorbis_comment
            if (!m_bVorbisInfoInited)
            {
                vorbis_info_init(&m_vorbisInfo);
                vorbis_comment_init(&m_vorbisComment);
                m_bVorbisInfoInited = true;
            }

            ogg_packet op{};
            op.packet     = pData;
            op.bytes      = (long)cbData;
            op.b_o_s      = (m_nHeaderPacketsReceived == 0) ? 1 : 0;
            op.e_o_s      = 0;
            op.granulepos = 0;
            op.packetno   = m_nHeaderPacketsReceived;

            int ret = vorbis_synthesis_headerin(&m_vorbisInfo, &m_vorbisComment, &op);
            pBuffer->Unlock();

            if (ret == 0)
            {
                m_nHeaderPacketsReceived++;
                if (m_nHeaderPacketsReceived == 3)
                {
                    // 3 个 header 全部就位，完成初始化
                    if (vorbis_synthesis_init(&m_vorbisDspState, &m_vorbisInfo) == 0 &&
                        vorbis_block_init(&m_vorbisDspState, &m_vorbisBlock) == 0)
                    {
                        m_uiChannels          = (UINT32)m_vorbisInfo.channels;
                        m_uiSampleRate        = (UINT32)m_vorbisInfo.rate;
                        m_bDecoderInitialized = true;
                    }
                }
            }
            return MF_E_TRANSFORM_NEED_MORE_INPUT;
        }

        // 还没收齐 header 包或这不是 header 包，无法解码
        pBuffer->Unlock();
        return MF_E_TRANSFORM_NEED_MORE_INPUT;
    }

    // -------------------------------------------------------
    // 正常音频解码路径
    // -------------------------------------------------------
    ogg_packet op{};
    op.packet      = pData;
    op.bytes       = (long)cbData;
    op.b_o_s       = 0;
    op.e_o_s       = 0;
    op.granulepos  = -1;
    op.packetno    = m_nHeaderPacketsReceived++; // 递增保证 libvorbis 内部序号正确

    // 若误发了一个 header 包（某些容器的异常行为），直接跳过
    if (IsVorbisHeaderPacket(pData, cbData))
    {
        pBuffer->Unlock();
        return MF_E_TRANSFORM_NEED_MORE_INPUT;
    }

    int ret = vorbis_synthesis(&m_vorbisBlock, &op);
    pBuffer->Unlock();

    if (ret != 0 && ret != OV_ENOTAUDIO)
        return E_FAIL;

    if (ret == OV_ENOTAUDIO)
    {
        // 意外收到头包（libvorbis 自行判断）
        return MF_E_TRANSFORM_NEED_MORE_INPUT;
    }

    vorbis_synthesis_blockin(&m_vorbisDspState, &m_vorbisBlock);

    // 收集所有解码出的 PCM 样本（交错）
    std::vector<float> interleavedPcm;
    float** pcm = nullptr;
    int samplesOut = 0;

    while ((samplesOut = vorbis_synthesis_pcmout(&m_vorbisDspState, &pcm)) > 0)
    {
        DWORD channels = m_uiChannels;
        size_t baseIdx = interleavedPcm.size();
        interleavedPcm.resize(baseIdx + (size_t)samplesOut * channels);

        float* dst = interleavedPcm.data() + baseIdx;
        for (int s = 0; s < samplesOut; s++)
        {
            for (DWORD ch = 0; ch < channels; ch++)
            {
                // libvorbis 输出范围 [-1.0, 1.0]，裁剪防止越界
                float v = pcm[ch][s];
                if (v >  1.0f) v =  1.0f;
                if (v < -1.0f) v = -1.0f;
                *dst++ = v;
            }
        }
        vorbis_synthesis_read(&m_vorbisDspState, samplesOut);
    }

    if (interleavedPcm.empty())
        return MF_E_TRANSFORM_NEED_MORE_INPUT;

    hr = CreateOutputSample(interleavedPcm, ppOutputSample);
    if (FAILED(hr)) return hr;

    // 传递时间戳和持续时间
    LONGLONG timestamp = 0;
    if (SUCCEEDED(pInputSample->GetSampleTime(&timestamp)))
        (*ppOutputSample)->SetSampleTime(timestamp);

    // 根据样本数计算精确的持续时间
    if (m_uiSampleRate > 0)
    {
        DWORD totalSamples = (DWORD)(interleavedPcm.size() / m_uiChannels);
        LONGLONG duration = (LONGLONG)totalSamples * 10000000LL / (LONGLONG)m_uiSampleRate;
        (*ppOutputSample)->SetSampleDuration(duration);
    }

    return S_OK;
}

// 创建 MF 音频 Sample
HRESULT VorbisDecoder::CreateOutputSample(
    const std::vector<float>& pcmData, IMFSample** ppSample)
{
    if (!ppSample || pcmData.empty())
        return E_POINTER;

    HRESULT hr;
    ComPtr<IMFSample>      pSample;
    ComPtr<IMFMediaBuffer> pBuffer;
    BYTE* pDest = nullptr;

    bool isFloat = (m_outputSubtype == MFAudioFormat_Float);
    DWORD bytesPerSample = isFloat ? 4 : 2;
    DWORD cbBuffer = (DWORD)(pcmData.size() * bytesPerSample);

    hr = MFCreateSample(&pSample);
    if (FAILED(hr)) return hr;

    hr = MFCreateMemoryBuffer(cbBuffer, &pBuffer);
    if (FAILED(hr)) return hr;

    hr = pBuffer->Lock(&pDest, nullptr, nullptr);
    if (FAILED(hr)) return hr;

    if (isFloat)
    {
        // 直接复制 float 数据
        memcpy(pDest, pcmData.data(), cbBuffer);
    }
    else
    {
        // float → 16-bit PCM
        ConvertFloatToInt16(
            pcmData.data(),
            reinterpret_cast<INT16*>(pDest),
            (DWORD)pcmData.size());
    }

    pBuffer->Unlock();
    pBuffer->SetCurrentLength(cbBuffer);

    hr = pSample->AddBuffer(pBuffer.Get());
    if (FAILED(hr)) return hr;

    *ppSample = pSample.Detach();
    return S_OK;
}

// float [-1,1] → int16 [-32768, 32767]
void VorbisDecoder::ConvertFloatToInt16(
    const float* pSrc, INT16* pDst, DWORD nSamples)
{
    for (DWORD i = 0; i < nSamples; i++)
    {
        float v = pSrc[i];
        // 缩放并四舍五入
        int32_t val = (int32_t)(v * 32767.0f + 0.5f);
        if      (val >  32767) val =  32767;
        else if (val < -32768) val = -32768;
        pDst[i] = (INT16)val;
    }
}


// ============================================================
// 媒体类型检验与设置
// ============================================================

HRESULT VorbisDecoder::OnCheckInputType(IMFMediaType* pType)
{
    if (!pType) return E_POINTER;

    GUID major = GUID_NULL, sub = GUID_NULL;
    if (FAILED(pType->GetGUID(MF_MT_MAJOR_TYPE, &major))) return MF_E_INVALIDMEDIATYPE;
    if (FAILED(pType->GetGUID(MF_MT_SUBTYPE, &sub)))      return MF_E_INVALIDMEDIATYPE;

    if (major != MFMediaType_Audio)   return MF_E_INVALIDMEDIATYPE;
    if (sub   != MFAudioFormat_Vorbis) return MF_E_INVALIDMEDIATYPE;

    return S_OK;
}

HRESULT VorbisDecoder::OnCheckOutputType(IMFMediaType* pType)
{
    if (!m_pInputType)  return MF_E_TRANSFORM_TYPE_NOT_SET;
    if (!pType)         return E_POINTER;

    GUID major = GUID_NULL, sub = GUID_NULL;
    if (FAILED(pType->GetGUID(MF_MT_MAJOR_TYPE, &major))) return MF_E_INVALIDMEDIATYPE;
    if (FAILED(pType->GetGUID(MF_MT_SUBTYPE, &sub)))      return MF_E_INVALIDMEDIATYPE;

    if (major != MFMediaType_Audio) return MF_E_INVALIDMEDIATYPE;

    bool found = false;
    for (DWORD i = 0; i < g_NumVorbisOutputFormats; i++)
    {
        if (sub == g_VorbisOutputFormats[i]) { found = true; break; }
    }
    if (!found) return MF_E_INVALIDMEDIATYPE;

    return S_OK;
}

HRESULT VorbisDecoder::OnSetInputType(IMFMediaType* pType)
{
    m_pInputType = pType;

    // 尝试从媒体类型属性中读取基本音频参数
    pType->GetUINT32(MF_MT_AUDIO_NUM_CHANNELS,        &m_uiChannels);
    pType->GetUINT32(MF_MT_AUDIO_SAMPLES_PER_SECOND,  &m_uiSampleRate);

    // 尝试从 MF_MT_USER_DATA 读取 Vorbis 私有数据并初始化解码器
    BYTE*  pUserData  = nullptr;
    UINT32 cbUserData = 0;

    if (SUCCEEDED(pType->GetAllocatedBlob(MF_MT_USER_DATA, &pUserData, &cbUserData))
        && cbUserData > 0)
    {
        InitializeDecoder(pUserData, cbUserData);
        CoTaskMemFree(pUserData);
    }

    return S_OK;
}

HRESULT VorbisDecoder::OnSetOutputType(IMFMediaType* pType)
{
    m_pOutputType = pType;
    pType->GetGUID(MF_MT_SUBTYPE, &m_outputSubtype);
    return S_OK;
}

HRESULT VorbisDecoder::OnFlush()
{
    m_pPendingSample.Reset();
    if (m_bDecoderInitialized)
    {
        // 重置 DSP 状态（清空内部缓冲区）
        vorbis_synthesis_restart(&m_vorbisDspState);
    }
    return S_OK;
}

HRESULT VorbisDecoder::OnDrain()
{
    m_pPendingSample.Reset();
    return S_OK;
}
