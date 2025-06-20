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

#pragma once

// Manages a list of allocated samples.
class SamplePool 
{
public:
  SamplePool();
  virtual ~SamplePool();

  HRESULT Initialize(VideoSampleList& samples);
  HRESULT Clear();
   
  HRESULT GetSample(IMFSample **ppSample);    // Does not block.
  HRESULT ReturnSample(IMFSample *pSample);   
  BOOL    AreSamplesPending();

private:
  CritSec           m_lock;
  VideoSampleList   m_VideoSampleQueue;       // Available queue
  BOOL              m_bInitialized;
  DWORD             m_cPending;
};