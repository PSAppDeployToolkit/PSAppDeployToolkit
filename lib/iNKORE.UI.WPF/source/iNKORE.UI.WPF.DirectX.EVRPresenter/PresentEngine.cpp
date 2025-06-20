//////////////////////////////////////////////////////////////////////////
//
// PresentEngine.cpp: Defines the D3DPresentEngine object.
//
// Based on a sample from(c) Microsoft Corporation. 
//
//
//////////////////////////////////////////////////////////////////////////

#include "EVRPresenter.h"
#include "MediaType.h"

#ifdef USEDX
#include <d3dx9tex.h>
#pragma comment(lib, "d3dx9.lib")
#endif

HRESULT FindAdapter(IDirect3D9 *pD3D9, HMONITOR hMonitor, UINT *puAdapterID);
HRESULT GetFourCC(IMFMediaType *pType, DWORD *pFourCC);
BOOL IsVistaOrLater();


//-----------------------------------------------------------------------------
// Constructor
//-----------------------------------------------------------------------------

D3DPresentEngine::D3DPresentEngine(HRESULT& hr) :
  m_hwnd(NULL),
  m_DeviceResetToken(0),
  m_pD3D9(NULL),
  m_pDevice(NULL),
  m_pDeviceManager(NULL),
  m_pRenderSurface(NULL),
  m_bufferCount(3),
  m_pCallback(NULL)
{

  LOG_MSG("Initialize D3DDevice")

    ZeroMemory(&m_DisplayMode, sizeof(m_DisplayMode));

  if(!IsVistaOrLater())
  {
    LOG_MSG("Error need Vista or later")
    hr = ERROR_BAD_ENVIRONMENT;
    return;
  }

  // Create Direct3D
  CHECK_HR( hr = Direct3DCreate9Ex(D3D_SDK_VERSION, &m_pD3D9));

  // Create the device manager
  CHECK_HR (hr = DXVA2CreateDirect3DDeviceManager9(&m_DeviceResetToken, &m_pDeviceManager));

  CHECK_HR (hr = CreateD3DDevice());

  LOG_MSG("PresentEngine: created")

done:
  return;
}


//-----------------------------------------------------------------------------
// Destructor
//-----------------------------------------------------------------------------

D3DPresentEngine::~D3DPresentEngine()
{
  ReleaseResources();
  SafeRelease(&m_pDevice);
  SafeRelease(&m_pDeviceManager);
  SafeRelease(&m_pD3D9);

  LOG_MSG("PresentEngine: terminated")
}


//-----------------------------------------------------------------------------
// GetService
//
// Returns a service interface from the presenter engine.
// The presenter calls this method from inside it's implementation of
// IMFGetService::GetService.
//
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::GetService(REFGUID guidService, REFIID riid, void** ppv)
{
  assert(ppv != NULL);

  HRESULT hr = MF_E_UNSUPPORTED_SERVICE;

  if (riid == __uuidof(IDirect3DDeviceManager9) && m_pDeviceManager != NULL)
  {
    *ppv = m_pDeviceManager;
    m_pDeviceManager->AddRef();
    hr = S_OK;
  }
  return hr;
}


//-----------------------------------------------------------------------------
// CheckFormat
//
// Queries whether the D3DPresentEngine can use a specified Direct3D format.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CheckFormat(D3DFORMAT format)
{
  HRESULT hr = S_OK;

  UINT uAdapter = D3DADAPTER_DEFAULT;
  D3DDEVTYPE type = D3DDEVTYPE_HAL;
  D3DDISPLAYMODE mode;
  D3DDEVICE_CREATION_PARAMETERS params;

  if (m_pDevice)
  {
    CHECK_HR(hr = m_pDevice->GetCreationParameters(&params));

    uAdapter = params.AdapterOrdinal;
    type = params.DeviceType;
  }

  CHECK_HR(hr = m_pD3D9->GetAdapterDisplayMode(uAdapter, &mode));
  CHECK_HR(hr = m_pD3D9->CheckDeviceType(uAdapter, type, mode.Format, format, TRUE)); 

done:
  return hr;
}



//-----------------------------------------------------------------------------
// SetVideoWindow
//
// this is only used to find the used adapter, not for drawing
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::SetVideoWindow(HWND hwnd)
{
  // Assertions: EVRCustomPresenter checks these cases.
  assert(IsWindow(hwnd));
  assert(hwnd != m_hwnd);

  AutoLock lock(m_ObjectLock);

  // Recreate the device.
  m_hwnd = hwnd;
  return CreateD3DDevice();
}


//-----------------------------------------------------------------------------
// CreateVideoSamples
//
// Creates video samples based on a specified media type.
//
// pFormat: Media type that describes the video format.
// videoSampleQueue: List that will contain the video samples.
//
// Note: For each video sample, the method creates a texture with a
// single back buffer. The video sample object holds a pointer to the texture's 
// back buffer surface. The mixer renders to this surface, and the
// D3DPresentEngine renders the video frame for presenting a rendertarget.
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CreateVideoSamples(IMFMediaType *pFormat, VideoSampleList& videoSampleQueue
                                             )
{
  if (m_hwnd == NULL)
    return MF_E_INVALIDREQUEST;

  if (pFormat == NULL)
    return MF_E_UNEXPECTED;

  LOG_MSG("Create VideoSamples")

    HRESULT hr = S_OK;
  D3DFORMAT d3dFormat = D3DFMT_UNKNOWN;
  D3DCOLOR clrBlack = D3DCOLOR_ARGB(0xFF, 0x00, 0x00, 0x00);
  UINT32 width, height;

  IMFSample *pVideoSample = NULL;            
  IDirect3DSurface9* pSurface = NULL;
  IDirect3DTexture9 *pTexture = NULL;

  AutoLock lock(m_ObjectLock);

  ReleaseResources();

  // Helper object for reading the proposed type.
  VideoType videoType(pFormat);

  // Get some information about the video format.
  CHECK_HR(hr = videoType.GetFrameDimensions(&width, &height));
  CHECK_HR(hr = videoType.GetFourCC((DWORD*)&d3dFormat));


  // Create the video samples.
  for (int bufferAllocated = 0; bufferAllocated < m_bufferCount; bufferAllocated++)
  {
    // Create a new texture (texture is untouched from graphic driver).
    CHECK_HR(hr = m_pDevice->CreateTexture(width, height, 1, D3DUSAGE_RENDERTARGET, d3dFormat, D3DPOOL_DEFAULT, &pTexture, NULL));

    // 1. Get the back buffer surface 
    CHECK_HR(pTexture->GetSurfaceLevel(0, &pSurface));

    // 2. Create the sample.
    CHECK_HR(hr =  MFCreateVideoSampleFromSurface(pSurface, &pVideoSample));

    // 3. Add it to the list.
    CHECK_HR(hr = videoSampleQueue.InsertBack(pVideoSample));

    // Set the swapchain pointer as a custom attribute on the sample. This keeps
    // a reference count on the swapchain, so that swapchain chain is kept alive
    // for the duration of the sample's lifetime.
    CHECK_HR(hr = pVideoSample->SetUnknown(MFSamplePresenter_SampleSwapChain, pTexture));


    SafeRelease(&pTexture);
    SafeRelease(&pVideoSample);
    SafeRelease(&pSurface);
  }

  // Create a renderSurface with the same size as our sample
  CHECK_HR(hr = m_pDevice->CreateRenderTarget(width, height, d3dFormat, D3DMULTISAMPLE_NONE, 0, TRUE, &m_pRenderSurface, NULL));
  CHECK_HR(hr = m_pDevice->ColorFill(m_pRenderSurface, NULL, clrBlack));

done:

  if (FAILED(hr)) 
  {
    LOG_IF_FAILED("Create VideoSamples ", hr);

    ReleaseResources();
    SafeRelease(&pTexture);
    SafeRelease(&pVideoSample);
    SafeRelease(&pSurface);
  }

  return hr;
}

//-----------------------------------------------------------------------------
// ReleaseResources
//
// Released Direct3D resources used by this object.
//-----------------------------------------------------------------------------

void D3DPresentEngine::ReleaseResources()
{
  SafeRelease(&m_pRenderSurface);
}

//-----------------------------------------------------------------------------
// GetDisplaySize
//
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::GetDisplaySize(SIZE *size)
{
  size->cx	= m_DisplayMode.Width;
  size->cy	= m_DisplayMode.Height;
  return S_OK;
}


//-----------------------------------------------------------------------------
// CheckDeviceState
//
// Tests the Direct3D device state.
//
// pState: Receives the state of the device (OK, reset, removed)
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CheckDeviceState(DeviceState *pState)
{
  HRESULT hr = S_OK;

  AutoLock lock(m_ObjectLock);

  *pState = DeviceOK;

  // Check the device state. Not every failure code is a critical failure.
  hr = m_pDevice->CheckDeviceState(m_hwnd);

  switch (hr)
  {
  case S_OK:
  case S_PRESENT_OCCLUDED:
  case S_PRESENT_MODE_CHANGED:
    // state is DeviceOK
    hr = S_OK;
    break;

  case D3DERR_DEVICELOST:
  case D3DERR_DEVICEHUNG:
    // Lost/hung device. Destroy the device and create a new one.
    CHECK_HR(hr = CreateD3DDevice());
    *pState = DeviceReset;
    break;

  case D3DERR_DEVICEREMOVED:
    // This is a fatal error.
    *pState = DeviceRemoved;
    break;

  case E_INVALIDARG:
    // CheckDeviceState can return E_INVALIDARG if the window is not valid
    // We'll assume that the window was destroyed; we'll recreate the device
    // if the application sets a new window.
    hr = S_OK;
  }

done:
  return hr;
}

//-----------------------------------------------------------------------------
// PresentSample
//
// Presents a video frame.
//
// pSample:  Pointer to the sample that contains the surface to present. If
//           this parameter is NULL, the method paints a (black rectangle) redraw from the last surface.
// llTarget: Target presentation time.
//
// This method is called by the scheduler and/or the presenter.
//-----------------------------------------------------------------------------

// Presents a video frame.
HRESULT D3DPresentEngine::PresentSample(IMFSample* pSample, LONGLONG llTarget)
{
  HRESULT hr = S_OK;
  HRESULT hrCopy = S_OK;
  IMFMediaBuffer* pBuffer = NULL;
  IDirect3DSurface9* pSurface = NULL;

  if (pSample)
  {

    // Get the buffer from the sample.
    hr = pSample->GetBufferByIndex(0, &pBuffer);
    if (SUCCEEDED(hr))
    {
      // Get the surface from the buffer.
      hr = MFGetService(pBuffer, MR_BUFFER_SERVICE, __uuidof(IDirect3DSurface9), (void**)&pSurface);
    }
    if (hr == D3DERR_DEVICELOST || hr == D3DERR_DEVICENOTRESET || hr == D3DERR_DEVICEHUNG)
    {
      // We failed because the device was lost.
      // This case is ignored. The Reset(Ex) method must be called from the thread that created the device.
      // The presenter will detect the state when it calls CheckDeviceState() on the next sample.
      hr = S_OK;
    }

    hrCopy = m_pDevice->StretchRect(pSurface,NULL,m_pRenderSurface,NULL,D3DTEXF_NONE);
  }
/*
  else // paint with black
  {
    D3DCOLOR clrBlack = D3DCOLOR_ARGB(0xFF, 0x00, 0x00, 0x00);
    hrCopy = m_pDevice->ColorFill(m_pRenderSurface, NULL, clrBlack);
  }
*/

  if(hrCopy == S_OK && m_pCallback)// Do the callback
    m_pCallback->PresentSurfaceCB(m_pRenderSurface);	


  SafeRelease(&pSurface);
  SafeRelease(&pBuffer);

  return hr;
}


//-----------------------------------------------------------------------------
// GetCurrentImage
//
// Get the ImageData from last surface (m_pSurfaceRepaint).
//
// pBih:   Pointer to the BITMAPINFOHEADER.
// pDib:   Pointer to a buffer that contains a packed Windows device-independent bitmap (DIB).
// pcbDib: Receives the size of the buffer returned in pDib, in bytes
// pTimeStamp: Always 0
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::GetCurrentImage(BITMAPINFOHEADER *pBih, BYTE **pDib, DWORD *pcbDib, LONGLONG *pTimeStamp)
{
  D3DSURFACE_DESC desc;
  HRESULT hr = S_OK;
  IDirect3DSurface9* pDestinationTargetSurface = NULL;

  AutoLock lock(m_ObjectLock);

  if(m_pRenderSurface == NULL) 
    return MF_E_SHUTDOWN;

  // Get the surface description
  if(FAILED(hr = m_pRenderSurface->GetDesc(&desc))) 
    return hr;

  // target Rectangle
  RECT rc = {0,0,desc.Width,desc.Height};

  //GetDestinationTargetSurface
  CHECK_HR(hr = m_pDevice->CreateOffscreenPlainSurface(desc.Width, desc.Height,
    m_DisplayMode.Format,
    D3DPOOL_SYSTEMMEM,
    &pDestinationTargetSurface,
    NULL));

  //copy RenderTargetSurface -> DestTarget (to SYSTEMMEM)
  CHECK_HR(hr = m_pDevice->GetRenderTargetData(m_pRenderSurface, pDestinationTargetSurface));

  //Create a lock on the DestTargetSurface
  D3DLOCKED_RECT lockedRC;
  CHECK_HR(hr = pDestinationTargetSurface->LockRect(&lockedRC,&rc,D3DLOCK_NO_DIRTY_UPDATE|D3DLOCK_NOSYSLOCK|D3DLOCK_READONLY));
  LPBYTE lpSource = reinterpret_cast<LPBYTE>(lockedRC.pBits);

  // create dest-mem and copy data
  DWORD dataSize = lockedRC.Pitch * (rc.bottom - rc.top) ;
  LPBYTE lpDestination = reinterpret_cast<LPBYTE>(CoTaskMemAlloc(dataSize));
  CheckPointer(lpDestination, E_OUTOFMEMORY);
  memcpy(lpDestination, lpSource, dataSize); 

  // set infos to BITMAPINFOHEADER
  pBih->biWidth=desc.Width;
  pBih->biHeight=desc.Height;
  pBih->biPlanes = 1;
  pBih->biCompression = BI_RGB;
  pBih->biBitCount=32;
  pBih->biSizeImage = dataSize;

  *pDib = lpDestination;
  *pcbDib = dataSize;
  *pTimeStamp = 0;

done:

  SafeRelease(&pDestinationTargetSurface);

  return hr;
}


//-----------------------------------------------------------------------------
// private/protected methods
//-----------------------------------------------------------------------------




//-----------------------------------------------------------------------------
// CreateD3DDevice
//
// Creates the Direct3D device.
//
// The caller must hold the lock because we might be discarding an exisiting device. 
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::CreateD3DDevice()
{
  LOG_MSG("CreateD3DDevice");

  if (!m_pD3D9 || !m_pDeviceManager) 
    return MF_E_NOT_INITIALIZED;

  HRESULT     hr = S_OK;
  HMONITOR    hMonitor = NULL;
  HWND        hwnd = NULL;
  UINT        uAdapterID = D3DADAPTER_DEFAULT;
  D3DCAPS9    ddCaps;
  ZeroMemory(&ddCaps, sizeof(ddCaps));

  IDirect3DDevice9Ex* pDevice = NULL;

  hwnd = GetDesktopWindow();

  // Note: The presenter creates additional swap chain to present the
  // video frames. Therefore, it does not use the device's implicit
  // swap chain, so the size of the back buffer here is 1 x 1.

  D3DPRESENT_PARAMETERS pp;

  ZeroMemory(&pp, sizeof(pp));

  pp.BackBufferWidth = 1;
  pp.BackBufferHeight = 1;
  pp.Windowed = TRUE;
  pp.SwapEffect = D3DSWAPEFFECT_COPY;
  pp.BackBufferFormat = D3DFMT_UNKNOWN; //D3DFMT_A8R8G8B8;
  pp.hDeviceWindow = hwnd;
  pp.Flags = D3DPRESENTFLAG_VIDEO;
  pp.PresentationInterval = D3DPRESENT_INTERVAL_DEFAULT;

  // Find the monitor for this window.
  if (m_hwnd)
  {
    hMonitor = MonitorFromWindow(m_hwnd, MONITOR_DEFAULTTONEAREST);

    // Find the corresponding adapter.
    CHECK_HR(hr = FindAdapter(m_pD3D9, hMonitor, &uAdapterID));
  }

  // Get the device caps for this adapter.
  CHECK_HR(hr = m_pD3D9->GetDeviceCaps(uAdapterID, D3DDEVTYPE_HAL, &ddCaps));
  DWORD vp = (ddCaps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT) ? D3DCREATE_HARDWARE_VERTEXPROCESSING : D3DCREATE_SOFTWARE_VERTEXPROCESSING;

  // Create the device.
  CHECK_HR(hr = m_pD3D9->CreateDeviceEx(
    uAdapterID,
    D3DDEVTYPE_HAL,
    pp.hDeviceWindow,
    vp | D3DCREATE_NOWINDOWCHANGES | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
    &pp, 
    NULL,
    &pDevice
    ));

  // Get the adapter display mode.
  CHECK_HR(hr = m_pD3D9->GetAdapterDisplayMode(uAdapterID, &m_DisplayMode));

  // Reset the D3DDeviceManager with the new device
  CHECK_HR(hr = m_pDeviceManager->ResetDevice(pDevice, m_DeviceResetToken));

  SafeRelease(&m_pDevice);

  m_pDevice = pDevice;
  m_pDevice->AddRef();

done:
  SafeRelease(&pDevice);
  LOG_IF_FAILED("CreateD3DDevice failed => hr=0x%X", hr);
  return hr;
}

//-----------------------------------------------------------------------------
// GetPresentParameters
//
// Given a media type that describes the video format, fills in the
// D3DPRESENT_PARAMETERS for creating a swap chain. 
//-----------------------------------------------------------------------------

HRESULT D3DPresentEngine::GetPresentParameters(IMFMediaType *pType, D3DPRESENT_PARAMETERS* pPP)
{
  // Caller holds the object lock.

  if (m_hwnd == NULL)
    return MF_E_INVALIDREQUEST;

  HRESULT hr = S_OK;

  UINT32 width = 0, height = 0;
  DWORD d3dFormat = D3DFMT_UNKNOWN;

  // Get some information about the video format.
  CHECK_HR(hr = MFGetAttributeSize(pType, MF_MT_FRAME_SIZE, &width, &height));
  CHECK_HR(hr = GetFourCC(pType, &d3dFormat));

  ZeroMemory(pPP, sizeof(D3DPRESENT_PARAMETERS));
  pPP->BackBufferWidth = width;
  pPP->BackBufferHeight = height;
  pPP->BackBufferCount = 1;
  pPP->Windowed = TRUE;
  pPP->BackBufferFormat = (D3DFORMAT)d3dFormat;
  pPP->hDeviceWindow = m_hwnd;
  pPP->Flags = D3DPRESENTFLAG_VIDEO;
  pPP->SwapEffect = D3DSWAPEFFECT_DISCARD;
  pPP->PresentationInterval = D3DPRESENT_INTERVAL_ONE;


  D3DDEVICE_CREATION_PARAMETERS params;
  CHECK_HR(hr = m_pDevice->GetCreationParameters(&params));
  if (params.DeviceType != D3DDEVTYPE_HAL)
    pPP->Flags |= D3DPRESENTFLAG_LOCKABLE_BACKBUFFER;

done:
  return hr;
}


//-----------------------------------------------------------------------------
// RegisterCallback
//
// Registers a callback sink for getting the D3D surface and
// is called for every video frame that needs to be rendered
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::RegisterCallback(IEVRPresenterCallback *pCallback)
{
  AutoLock lock(m_ObjectLock);

  if(m_pCallback) 
    SafeRelease(&m_pCallback);

  m_pCallback = pCallback;

  if(m_pCallback)
    m_pCallback->AddRef();

  return S_OK;
}


//-----------------------------------------------------------------------------
// SetBufferCount
//
// Sets the total number of buffers to use when the EVR
// custom presenter is running.
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::SetBufferCount(INT bufferCount)
{
	if(bufferCount <= 2)
		return E_INVALIDARG;

	m_bufferCount = bufferCount;
	return S_OK;
}

//-----------------------------------------------------------------------------
// GetBufferCount
//
// Get the total number of buffers to use when the EVR
// custom presenter is running.
//-----------------------------------------------------------------------------
HRESULT D3DPresentEngine::GetBufferCount(INT* bufferCount)
{
	CheckPointer(bufferCount, E_POINTER);
	*bufferCount = m_bufferCount;
	return S_OK;
}


//-----------------------------------------------------------------------------
// Static functions
//-----------------------------------------------------------------------------

BOOL IsVistaOrLater()
{
  BOOL bOsVersionInfoEx;
  OSVERSIONINFOEX  osvi;	
  ZeroMemory(&osvi, sizeof(OSVERSIONINFOEX));
  osvi.dwOSVersionInfoSize = sizeof(OSVERSIONINFOEX);

  bOsVersionInfoEx = GetVersionEx((OSVERSIONINFO*) &osvi);
  if( ! bOsVersionInfoEx ) 
    return FALSE;

  return osvi.dwMajorVersion >= 6;
}

//-----------------------------------------------------------------------------
// FindAdapter
//
// Given a handle to a monitor, returns the ordinal number that D3D uses to
// identify the adapter.
//-----------------------------------------------------------------------------

HRESULT FindAdapter(IDirect3D9 *pD3D9, HMONITOR hMonitor, UINT *puAdapterID)
{
  HRESULT hr = E_FAIL;
  UINT cAdapters = 0;
  UINT uAdapterID = (UINT)-1;

  cAdapters = pD3D9->GetAdapterCount();
  for (UINT i = 0; i < cAdapters; i++)
  {
    HMONITOR hMonitorTmp = pD3D9->GetAdapterMonitor(i);

    if (hMonitorTmp == NULL)
      break;

    if (hMonitorTmp == hMonitor)
    {
      uAdapterID = i;
      break;
    }
  }

  if (uAdapterID != (UINT)-1)
  {
    *puAdapterID = uAdapterID;
    hr = S_OK;
  }
  return hr;
}

//-----------------------------------------------------------------------------
// GetFourCC
//
// Extracts the FOURCC code from the subtype.
// Not all subtypes follow this pattern.
//-----------------------------------------------------------------------------

HRESULT GetFourCC(IMFMediaType *pType, DWORD *pFourCC)
{
  if (pFourCC == NULL) 
    return E_POINTER; 

  GUID guidSubType = GUID_NULL;

  HRESULT hr = pType->GetGUID(MF_MT_SUBTYPE, &guidSubType);
  if (SUCCEEDED(hr))
    *pFourCC = guidSubType.Data1;

  return hr;
}