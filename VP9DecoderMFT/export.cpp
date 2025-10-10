#pragma once
#include <windows.h>
#include <mfapi.h>
#include "pch.h"
#include "VP9Decoder.h"

// --- Class Factory for VP9Decoder ---

class VP9DecoderClassFactory : public IClassFactory
{
public:
	// IUnknown
	STDMETHODIMP QueryInterface(REFIID riid, void** ppv)
	{
		if (!ppv) return E_POINTER;
		if (riid == IID_IUnknown || riid == IID_IClassFactory)
		{
			*ppv = this;
			AddRef();
			return S_OK;
		}
		*ppv = nullptr;
		return E_NOINTERFACE;
	}
	STDMETHODIMP_(ULONG) AddRef() { return InterlockedIncrement(&m_cRef); }
	STDMETHODIMP_(ULONG) Release()
	{
		long cRef = InterlockedDecrement(&m_cRef);
		if (cRef == 0) delete this;
		return cRef;
	}

	// IClassFactory
	STDMETHODIMP CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppv)
	{
		if (pUnkOuter != nullptr) return CLASS_E_NOAGGREGATION;

		VP9Decoder* pDecoder = new (std::nothrow) VP9Decoder();
		if (!pDecoder) return E_OUTOFMEMORY;

		HRESULT hr = pDecoder->QueryInterface(riid, ppv);
		pDecoder->Release(); // QueryInterface adds a ref, so we release our initial one.
		return hr;
	}
	STDMETHODIMP LockServer(BOOL fLock) { return S_OK; }

	VP9DecoderClassFactory() : m_cRef(1) {}

private:
	~VP9DecoderClassFactory() {}
	long m_cRef;
};


// Global pointer to the class factory
IClassFactory* g_pClassFactory = nullptr;


extern "C" __declspec(dllexport) HRESULT RegisterVP9DecoderLocal()
{
	if (!g_pClassFactory)
	{
		g_pClassFactory = new (std::nothrow) VP9DecoderClassFactory();
	}
	MFT_REGISTER_TYPE_INFO inputTypes[] = {
		   { MFMediaType_Video, MFVideoFormat_VP90 }
	};
	MFT_REGISTER_TYPE_INFO outputTypes[] = {
		//{ MFMediaType_Video, MFVideoFormat_NV12 },
		{ MFMediaType_Video, MFVideoFormat_I420 },
		//{ MFMediaType_Video, MFVideoFormat_P016 },
	};

	HRESULT hr = MFTRegisterLocal(
		g_pClassFactory,
		MFT_CATEGORY_VIDEO_DECODER,
		(LPWSTR)L"VP9 Profile 3 Video Decoder",
		MFT_ENUM_FLAG_SYNCMFT | MFT_ENUM_FLAG_LOCALMFT,
		ARRAYSIZE(inputTypes),
		inputTypes,
		ARRAYSIZE(outputTypes),
		outputTypes
	);
	
	return hr;
}


extern "C" __declspec(dllexport) HRESULT UnregisterVP9DecoderLocal()
{
	if (g_pClassFactory)
	{
		HRESULT hr = MFTUnregisterLocal(g_pClassFactory);
		g_pClassFactory->Release();
		g_pClassFactory = nullptr;
		return hr;
	}
	return S_OK;
}
