//////////////////////////////////////////////////////////////////////////
//
// Scheduler.cpp: Schedules when video frames are presented.
//
// Based on a sample from(c) Microsoft Corporation. 
//
//
//////////////////////////////////////////////////////////////////////////

#include "EVRPresenter.h"

const MFTIME ONE_SECOND = 10000000; // One second.
const LONG   ONE_MSEC = 1000;       // One millisecond

// ScheduleEvent
// Messages for the scheduler thread.
enum ScheduleEvent
{
  eTerminate =    WM_USER,
  eSchedule =     WM_USER + 1,
  eFlush =        WM_USER + 2
};


// Convert 100-nanosecond units to milliseconds.

inline LONG MFTimeToMsec(const LONGLONG& time)
{
  return (LONG)(time / (ONE_SECOND / ONE_MSEC));
}


const DWORD SCHEDULER_TIMEOUT = 2000;

//-----------------------------------------------------------------------------
// Constructor
//-----------------------------------------------------------------------------

Scheduler::Scheduler() :
  m_pCB(NULL),
  m_pClock(NULL),
  m_dwThreadID(0),
  m_hSchedulerThread(NULL),
  m_hThreadReadyEvent(NULL),
  m_hFlushDoneEvent(NULL),
  m_hThreadTerminateEvent(NULL),
  m_hFlushEvent(NULL),
  m_hThreadScheduleEvent(NULL),
  m_fRate(1.0f),
  m_LastSampleTime(0),
  m_PerFrameInterval(0),
  m_PerFrame_1_4th(0)
{
}


//-----------------------------------------------------------------------------
// Destructor
//-----------------------------------------------------------------------------

Scheduler::~Scheduler()
{
  SafeRelease(&m_pClock);
}


//-----------------------------------------------------------------------------
// SetFrameRate
// Specifies the frame rate of the video, in frames per second.
//-----------------------------------------------------------------------------

void Scheduler::SetFrameRate(const MFRatio& fps)
{
  UINT64 AvgTimePerFrame = 0;

  // Convert to a duration.
  MFFrameRateToAverageTimePerFrame(fps.Numerator, fps.Denominator, &AvgTimePerFrame);

  m_PerFrameInterval = (MFTIME)AvgTimePerFrame;

  // Calculate 1/4th of this value, because we use it frequently.
  m_PerFrame_1_4th = m_PerFrameInterval / 4;
}



//-----------------------------------------------------------------------------
// StartScheduler
// Starts the scheduler's worker thread.
//
// IMFClock: Pointer to the EVR's presentation clock. Can be NULL.
//-----------------------------------------------------------------------------

HRESULT Scheduler::StartScheduler(IMFClock *pClock)
{
  LOG_MSG("StartScheduler");

  if (m_hSchedulerThread != NULL)
    return E_UNEXPECTED;

  HRESULT hr = S_OK;
  DWORD dwID = 0;

  CopyComPointer(m_pClock, pClock);

  // Set a high the timer resolution (ie, short timer period).
  timeBeginPeriod(1);

  // Create an event to wait for the thread to start.
  m_hThreadReadyEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
  if (m_hThreadReadyEvent == NULL)
  {
    hr = HRESULT_FROM_WIN32(GetLastError());
    goto done;
  }

  // Create an event to wait for flush commands to complete.
  m_hFlushDoneEvent = CreateEvent(NULL, FALSE, FALSE, NULL);
  if (m_hFlushDoneEvent == NULL)
  {
    hr = HRESULT_FROM_WIN32(GetLastError());
    goto done;
  }


  // Create the scheduler thread.
  m_hSchedulerThread = CreateThread(NULL, 0, SchedulerThreadProc, (LPVOID)this, 0, &dwID);
  if (m_hSchedulerThread == NULL)
  {
    hr = HRESULT_FROM_WIN32(GetLastError());
    goto done;
  }

  //	SetThreadPriority(m_hSchedulerThread, THREAD_PRIORITY_TIME_CRITICAL);


  HANDLE hObjects[] = { m_hThreadReadyEvent, m_hSchedulerThread };
  DWORD dwWait = 0;

  // Wait for the thread to signal the "thread ready" event.
  dwWait = WaitForMultipleObjects(2, hObjects, FALSE, INFINITE);  // Wait for EITHER of these handles.
  if (WAIT_OBJECT_0 != dwWait)
  {
    // The thread terminated early for some reason. This is an error condition.
    CloseHandle(m_hSchedulerThread);
    m_hSchedulerThread = NULL;

    hr = E_UNEXPECTED;
    goto done;
  }

  m_dwThreadID = dwID;

done:
  // Regardless success/failure, we are done using the "thread ready" event.
  if (m_hThreadReadyEvent)
  {
    CloseHandle(m_hThreadReadyEvent);
    m_hThreadReadyEvent = NULL;
  }

  LOG_MSG("Scheduler started");

  return hr;
}


//-----------------------------------------------------------------------------
// StopScheduler
//
// Stops the scheduler's worker thread.
//-----------------------------------------------------------------------------

HRESULT Scheduler::StopScheduler()
{

  if (m_hSchedulerThread == NULL)
    return S_OK;

  LOG_MSG("StopScheduler");


  // Ask the scheduler thread to exit.
  PostThreadMessage(m_dwThreadID, eTerminate, 0, 0);

  // Wait for the thread to exit.
  WaitForSingleObject(m_hSchedulerThread, 500);

  /*
     // Discard samples.
  m_ScheduledSamples.Clear();
*/
  // Close handles.
  CloseHandle(m_hSchedulerThread);
  m_hSchedulerThread = NULL;

  CloseHandle(m_hFlushDoneEvent);
  m_hFlushDoneEvent = NULL;


  // Restore the timer resolution.
  timeEndPeriod(1);


  LOG_MSG("Scheduler stoped.");

  return S_OK;

}


//-----------------------------------------------------------------------------
// Flush
//
// Flushes all samples that are queued for presentation.
//
// Note: This method is synchronous; ie., it waits for the flush operation to
// complete on the worker thread.
//-----------------------------------------------------------------------------

HRESULT Scheduler::Flush()
{
  if (m_hSchedulerThread)
  {
    LOG_MSG("Flush");

    // Ask the scheduler thread to flush.
    PostThreadMessage(m_dwThreadID, eFlush, 0 , 0);

    // Wait for the scheduler thread to signal the flush event,
    // OR for the thread to terminate.
    HANDLE objects[] = { m_hFlushDoneEvent, m_hSchedulerThread };
    WaitForMultipleObjects(ARRAYSIZE(objects), objects, FALSE, SCHEDULER_TIMEOUT);
  }

  return S_OK;
}


//-----------------------------------------------------------------------------
// ScheduleSample
//
// Schedules a new sample for presentation.
//
// pSample:     Pointer to the sample.
// bPresentNow: If TRUE, the sample is presented immediately. Otherwise, the
//              sample's time stamp is used to schedule the sample.
//-----------------------------------------------------------------------------

HRESULT Scheduler::ScheduleSample(IMFSample *pSample, BOOL bPresentNow)
{
  if (m_pCB == NULL || m_hSchedulerThread == NULL)
  {
    return MF_E_NOT_INITIALIZED;
  }


  HRESULT hr = S_OK;
  DWORD dwExitCode = 0;

  GetExitCodeThread(m_hSchedulerThread, &dwExitCode);
  if (dwExitCode != STILL_ACTIVE)
  {
    return E_FAIL;
  }

  if (bPresentNow || (m_pClock == NULL))
  {
    // Present the sample immediately.
    m_pCB->PresentSample(pSample, 0);
  }
  else
  {
    // Queue the sample and ask the scheduler thread to wake up.
    hr = m_ScheduledSamples.Queue(pSample);
    if (SUCCEEDED(hr))
    {
      PostThreadMessage(m_dwThreadID, eSchedule, 0, 0);
    }
  }

  return hr;
}

//-----------------------------------------------------------------------------
// ProcessSamplesInQueue
//
// Processes all the samples in the queue.
//
// plNextSleep: Receives the length of time the scheduler thread should sleep
//              before it calls ProcessSamplesInQueue again.
//-----------------------------------------------------------------------------


// Processes all the samples in the queue.
HRESULT Scheduler::ProcessSamplesInQueue(LONG *plNextSleep)
{
  HRESULT hr = S_OK;
  LONG lWait = 0;
  IMFSample *pSample = NULL;

  // Process samples until the queue is empty or until the wait time > 0.

  while (true) 
  {
    // Process the next sample in the queue. If the sample is not ready
    // for presentation. the value returned in lWait is > 0, which
    // means the scheduler should sleep for that amount of time.

    if (!ProcessSample(&lWait))
    {
      break;
    }
    if (lWait > 0)
    {
      break;
    }
  }

  // If the wait time is zero, it means we stopped because the queue is
  // empty (or an error occurred). Set the wait time to infinite; this will
  // make the scheduler thread sleep until it gets another thread message.
  if (lWait == 0)
    lWait = INFINITE;

  *plNextSleep = lWait;
  return hr;
}



//-----------------------------------------------------------------------------
// ProcessSample
//
// Processes a sample.
//
// plNextSleep: Receives the length of time the scheduler thread should sleep.
//-----------------------------------------------------------------------------

bool Scheduler::ProcessSample(LONG *plNextSleep)
{
  HRESULT hr = S_OK;

  LONGLONG hnsPresentationTime = 0;
  LONGLONG hnsTimeNow = 0;
  MFTIME   hnsSystemTime = 0;

  BOOL bPresentNow = TRUE;
  LONG lNextSleep = 0;

  IMFSample *pSample;
  // Note: Dequeue returns S_FALSE when the queue is empty.
  hr = m_ScheduledSamples.Dequeue(&pSample);
  if (hr != S_OK)
    return false;

  if (m_pClock)
  {
    // Get the sample's time stamp. It is valid for a sample to
    // have no time stamp.
    hr = pSample->GetSampleTime(&hnsPresentationTime);

    // Get the clock time. (But if the sample does not have a time stamp, 
    // we don't need the clock time.)
    if (SUCCEEDED(hr))
    {
      hr = m_pClock->GetCorrelatedTime(0, &hnsTimeNow, &hnsSystemTime);

      // Calculate the time until the sample's presentation time. 
      // A negative value means the sample is late.
      LONGLONG hnsDelta = hnsPresentationTime - hnsTimeNow;
      if (m_fRate < 0)
      {
        // For reverse playback, the clock runs backward. Therefore the delta is reversed.
        hnsDelta = -hnsDelta;
      }

      if (hnsDelta < 0)
      {
        // This sample is late.
        bPresentNow = TRUE;
      }
      else if (hnsDelta > m_PerFrame_1_4th)
      {
        // This sample is still too early. Go to sleep.
        lNextSleep = MFTimeToMsec(hnsDelta - m_PerFrame_1_4th);

        // Adjust the sleep time for the clock rate. (The presentation clock runs
        // at m_fRate, but sleeping uses the system clock.)
        lNextSleep = (LONG)(lNextSleep / fabsf(m_fRate));

        // Don't present yet.
        bPresentNow = FALSE;
      }
    }
  }

  if (bPresentNow)
  {
    hr = m_pCB->PresentSample(pSample, hnsPresentationTime);
  }
  else
  {
    // The sample is not ready yet. Return it to the queue.
    hr = m_ScheduledSamples.PutBack(pSample);
  }

  *plNextSleep = lNextSleep;

  SafeRelease(&pSample);
  return true;
}

//-----------------------------------------------------------------------------
// SchedulerThreadProc (static method)
//
// ThreadProc for the scheduler thread.
//-----------------------------------------------------------------------------

DWORD WINAPI Scheduler::SchedulerThreadProc(LPVOID lpParameter)
{
  Scheduler* pScheduler = reinterpret_cast<Scheduler*>(lpParameter);
  if (pScheduler == NULL)
    return -1;

  return pScheduler->SchedulerThreadProcPrivate();
}

//-----------------------------------------------------------------------------
// SchedulerThreadProcPrivate
//
// Non-static version of the ThreadProc.
//-----------------------------------------------------------------------------

DWORD Scheduler::SchedulerThreadProcPrivate()
{
  HRESULT hr = S_OK;
  MSG     msg;
  LONG    lWait = INFINITE;
  BOOL    bExitThread = FALSE;


  // Force the system to create a message queue for this thread.
  // (See MSDN documentation for PostThreadMessage.)
  PeekMessage(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);

  // Signal to the scheduler that the thread is ready.
  SetEvent(m_hThreadReadyEvent);

  while( !bExitThread )
  {
	  // Wait for a thread message OR until the wait time expires.
	  DWORD dwResult = MsgWaitForMultipleObjects(0, NULL, FALSE, lWait, QS_POSTMESSAGE);

	  if (dwResult == WAIT_TIMEOUT)
	  {
		  // If we timed out, then process the sample in the queue
		  hr = ProcessSamplesInQueue(&lWait);
		  if (FAILED(hr))
			  bExitThread = TRUE;
	  }

	  while (PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
	  {
		  switch (msg.message) 
		  {
		  case eTerminate:
			  LOG_MSG("eTerminate");
			  // Flushing: Clear the sample queue and set the event.
			  m_ScheduledSamples.Clear();
			  bExitThread = TRUE;
			  break;

		  case eFlush:
			  LOG_MSG("eFlush");
			  // Flushing: Clear the sample queue and set the event.
			  m_ScheduledSamples.Clear();
			  lWait = INFINITE;
			  SetEvent(m_hFlushDoneEvent);
			  break;

		  case eSchedule:
			  // try to process the sample, lWait contains the next timeout value for deliver.
			  hr = ProcessSamplesInQueue(&lWait);
			  if (FAILED(hr))
				  bExitThread = TRUE;
			  break;
		  }

	  } // while PeekMessage

  }  // while (!bExitThread)


  return (SUCCEEDED(hr) ? 0 : 1);
}

