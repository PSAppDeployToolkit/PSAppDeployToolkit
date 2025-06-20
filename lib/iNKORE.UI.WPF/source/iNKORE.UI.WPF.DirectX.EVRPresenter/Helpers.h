//////////////////////////////////////////////////////////////////////////
//
// Helpers.cpp : Miscellaneous helpers.
//
// Based on a sample from(c) Microsoft Corporation. 
//
//
//////////////////////////////////////////////////////////////////////////

#pragma once

// CopyComPointer
// Assigns a COM pointer to another COM pointer.
template <class T>
void CopyComPointer(T* &dest, T *src)
{
    if (dest)
    {
        dest->Release();
    }
    dest = src;
    if (dest)
    {
        dest->AddRef();
    }
}

template <class T> 
void SafeRelease(T **ppT)
{
    if (*ppT)
    {
        (*ppT)->Release();
        *ppT = NULL;
    }
}


// Tests two COM pointers for equality.
template <class T1, class T2>
bool AreComObjectsEqual(T1 *p1, T2 *p2)
{
  bool bResult = false;
  if (p1 == NULL && p2 == NULL)
  {
    // Both are NULL
    bResult = true;
  }
  else if (p1 == NULL || p2 == NULL)
  {
    // One is NULL and one is not
    bResult = false;
  }
  else 
  {
    // Both are not NULL. Compare IUnknowns.
    IUnknown *pUnk1 = NULL;
    IUnknown *pUnk2 = NULL;
    if (SUCCEEDED(p1->QueryInterface(IID_IUnknown, (void**)&pUnk1)))
    {
      if (SUCCEEDED(p2->QueryInterface(IID_IUnknown, (void**)&pUnk2)))
      {
        bResult = (pUnk1 == pUnk2);
        pUnk2->Release();
      }
      pUnk1->Release();
    }
  }
 
  return bResult;
}


//-----------------------------------------------------------------------------
// ThreadSafeQueue template
// Thread-safe queue of COM interface pointers.
//
// T: COM interface type.
//
// This class is used by the scheduler.
//
// Note: This class uses a critical section to protect the state of the queue.
// With a little work, the scheduler could probably use a lock-free queue.
//-----------------------------------------------------------------------------

template <class T>
class ThreadSafeQueue
{
public:

	HRESULT Queue(T *p)
	{
		AutoLock lock(m_lock);
		return m_list.InsertBack(p);
	}

	BOOL IsEmpty()
	{
		AutoLock lock(m_lock);

		m_list.IsEmpty()
	}

	HRESULT Dequeue(T **pp)
	{
		AutoLock lock(m_lock);

		if (m_list.IsEmpty())
		{
			*pp = NULL;
			return S_FALSE;
		}
		else
		{
			return m_list.RemoveFront(pp);
		}
	}

	HRESULT PutBack(T *p)
	{		
		AutoLock lock(m_lock);
		return m_list.InsertFront(p);
	}

	void Clear()
	{
		AutoLock lock(m_lock);
		m_list.Clear();
	}

private:
	CritSec				m_lock;	
	ComPtrList<T>		m_list;
};

//#define FILE_LOGGING

#if defined FILE_LOGGING

static void Log(const char *fmt, ...) 
{
	va_list ap;
	char buffer[1000]; 

	va_start(ap,fmt);
	int tmp=vsprintf_s(buffer, fmt, ap);
	va_end(ap); 

	FILE* fp = fopen("EVRPresenter.log","a+");
	if (fp!=NULL)
	{
		SYSTEMTIME systemTime;
		GetLocalTime(&systemTime);
		fprintf(fp,"%02.2d-%02.2d-%04.4d %02.2d:%02.2d:%02.2d.%03.3d [%x]%s\n",
			systemTime.wDay, systemTime.wMonth, systemTime.wYear,
			systemTime.wHour,systemTime.wMinute,systemTime.wSecond,
			systemTime.wMilliseconds,
			GetCurrentThreadId(),
			buffer);
		fclose(fp);
	}
};

static void LogGUID(const char *msg, REFGUID guid ) 
{
	// convert CLSID uuid to string
    OLECHAR szCLSID[CHARS_IN_GUID];
    HRESULT hr = StringFromGUID2(guid, szCLSID, CHARS_IN_GUID);
	Log("%s %ls", msg,  szCLSID);
}


#define LOG_MSG(msg) Log((msg)); 
#define LOG_GUID(msg, guid) LogGUID(msg, guid);
#define LOG_IF_FAILED(msg, hr) if (FAILED(hr)) { Log(msg,hr); }
#define LOG_HR(msg, hr) Log(msg,hr);

#else


#define LOG_MSG(msg)  
#define LOG_GUID(msg, guid) 
#define LOG_IF_FAILED(msg, hr) 
#define LOG_HR(msg, hr)
#endif



