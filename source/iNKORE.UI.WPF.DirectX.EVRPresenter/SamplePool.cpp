//////////////////////////////////////////////////////////////////////////
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
//////////////////////////////////////////////////////////////////////////

#include "EVRPresenter.h"

// Constructor
SamplePool::SamplePool() : m_bInitialized(FALSE), m_cPending(0)
{
}

// Destructor
SamplePool::~SamplePool()
{
}


// Gets a sample from the pool. If no samples are available, the method returns MF_E_SAMPLEALLOCATOR_EMPTY.
HRESULT SamplePool::GetSample(IMFSample **ppSample)
{
  AutoLock lock(m_lock);

  if (!m_bInitialized)
    return MF_E_NOT_INITIALIZED;

  if (m_VideoSampleQueue.IsEmpty())
    return MF_E_SAMPLEALLOCATOR_EMPTY;

  // Get a sample from the allocated queue.

  // It doesn't matter if we pull them from the head or tail of the list,
  // but when we get it back, we want to re-insert it onto the opposite end.
  // (see ReturnSample)

  IMFSample *pSample = NULL;

  HRESULT hr = m_VideoSampleQueue.RemoveFront(&pSample);
  if (SUCCEEDED(hr))
  {
	  m_cPending++;

	  // Give the sample to the caller.
	  *ppSample = pSample;
	  (*ppSample)->AddRef();
  }

  SafeRelease(&pSample);
  return hr;
}


// Returns a sample to the pool.
HRESULT SamplePool::ReturnSample(IMFSample *pSample) 
{
	AutoLock lock(m_lock);

	if (!m_bInitialized)
		return MF_E_NOT_INITIALIZED;

	HRESULT hr = m_VideoSampleQueue.InsertBack(pSample);
	if (SUCCEEDED(hr))
	{
		m_cPending--;
	}

	return hr;
}


// Returns TRUE if any samples are in use.
BOOL SamplePool::AreSamplesPending()
{
  AutoLock lock(m_lock);

  return m_bInitialized ? (m_cPending > 0) : FALSE;
}


// Initializes the pool with a list of samples.
HRESULT SamplePool::Initialize(VideoSampleList& samples)
{
	LOG_MSG("Initialize samplepool");

	AutoLock lock(m_lock);

    if (m_bInitialized) return MF_E_INVALIDREQUEST;

    HRESULT hr = S_OK;

    IMFSample *pSample = NULL;

    // Move these samples into our allocated queue.
    VideoSampleList::POSITION pos = samples.FrontPosition();
    while (pos != samples.EndPosition())
    {
		CHECK_HR(hr = samples.GetItemPos(pos, &pSample));        
        CHECK_HR(hr = m_VideoSampleQueue.InsertBack(pSample));

        pos = samples.Next(pos);
        SafeRelease(&pSample);
    }

    m_bInitialized = TRUE;

done:
    samples.Clear();
    SafeRelease(&pSample);
    return hr;
}


// Releases all samples.
HRESULT SamplePool::Clear()
{
  HRESULT hr = S_OK;

  AutoLock lock(m_lock);

  m_VideoSampleQueue.Clear();
  m_bInitialized = FALSE;
  m_cPending = 0;

  return hr;
}

