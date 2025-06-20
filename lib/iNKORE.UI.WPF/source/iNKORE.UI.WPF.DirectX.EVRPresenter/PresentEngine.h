//////////////////////////////////////////////////////////////////////////
//
// PresentEngine.h: Defines the D3DPresentEngine object.
//
// Based on a sample from(c) Microsoft Corporation. 
//
//////////////////////////////////////////////////////////////////////////

#pragma once

//#define USEDX

//-----------------------------------------------------------------------------
// D3DPresentEngine class
//
// This class creates the Direct3D device, allocates Direct3D surfaces for
// rendering, and presents the surfaces. This class also owns the Direct3D
// device manager and provides the IDirect3DDeviceManager9 interface via
// GetService.
//
// The goal of this class is to isolate the EVRCustomPresenter class from
// the details of Direct3D as much as possible.
//-----------------------------------------------------------------------------

class D3DPresentEngine : public SchedulerCallback
{
public:

	// State of the Direct3D device.
	enum DeviceState
	{
		DeviceOK,
		DeviceReset,    // The device was reset OR re-created.
		DeviceRemoved,  // The device was removed.
	};

	D3DPresentEngine(HRESULT& hr);
	virtual ~D3DPresentEngine();

	// IEVRPresenterSettings methods
	HRESULT SetBufferCount(int bufferCount);
	HRESULT GetBufferCount(int* bufferCount);
	HRESULT RegisterCallback(IEVRPresenterCallback *pCallback);

	// GetService: Returns the IDirect3DDeviceManager9 interface.
	// (The signature is identical to IMFGetService::GetService but
	// this object does not derive from IUnknown.)
	virtual HRESULT GetService(REFGUID guidService, REFIID riid, void** ppv);

	virtual HRESULT CheckFormat(D3DFORMAT format);

	// Video window / destination rectangle:
	// This object implements a sub-set of the functions defined by the
	// IMFVideoDisplayControl interface. However, some of the method signatures
	// are different. The presenter's implementation of IMFVideoDisplayControl
	// calls these methods.
	HRESULT SetVideoWindow(HWND hwnd);
	HWND    GetVideoWindow() const { return m_hwnd; }
	HRESULT GetCurrentImage(BITMAPINFOHEADER *pBih, BYTE **pDib, DWORD *pcbDib, LONGLONG *pTimeStamp);

	HRESULT CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue);
	void    ReleaseResources();

	HRESULT CheckDeviceState(DeviceState *pState);
	HRESULT PresentSample(IMFSample* pSample, LONGLONG llTarget);

	UINT    RefreshRate() const { return m_DisplayMode.RefreshRate; }
	HRESULT	GetDisplaySize(SIZE *size);


protected:
	HRESULT GetPresentParameters(IMFMediaType *pType, D3DPRESENT_PARAMETERS* pPP);
	HRESULT CreateD3DDevice();
	static DWORD WINAPI DeliveryThreadProc(LPVOID lpParameter);

protected:

	UINT                        m_DeviceResetToken;     // Reset token for the D3D device manager.
	HWND                        m_hwnd;                 // Application-provided destination window.
	D3DDISPLAYMODE              m_DisplayMode;          // Adapter's display mode.

	IEVRPresenterCallback				*m_pCallback;
	INT													m_bufferCount;

	CritSec											m_ObjectLock;           // Thread lock for the D3D device.

	// COM interfaces
	IDirect3D9Ex                *m_pD3D9;
	IDirect3DDevice9Ex          *m_pDevice;
	IDirect3DDeviceManager9     *m_pDeviceManager;        // Direct3D device manager.
	IDirect3DSurface9						*m_pRenderSurface;

};