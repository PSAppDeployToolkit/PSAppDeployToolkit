#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#ifndef WIN32_NO_STATUS
#define WIN32_NO_STATUS
#endif
#include <Windows.h>
#undef WIN32_NO_STATUS

#include "PSADT.Native.h"

#pragma comment(lib, "kernel32.lib")
#pragma comment(lib, "ntdll.lib")

// Structure to hold parameters for the thread function.
struct NtQueryObjectParameters
{
    HANDLE Handle;
    OBJECT_INFORMATION_CLASS ObjectInformationClass;
    PVOID ObjectInformation;
    ULONG ObjectInformationLength;
    PULONG ReturnLength;
};

// Declarations for the necessary NT API functions.
extern "C" NTSYSAPI NTSTATUS NTAPI NtCreateThreadEx(
    PHANDLE ThreadHandle,
    ACCESS_MASK DesiredAccess,
    POBJECT_ATTRIBUTES ObjectAttributes,
    HANDLE ProcessHandle,
    PVOID StartRoutine,
    PVOID Argument,
    ULONG CreateFlags,
    SIZE_T ZeroBits,
    SIZE_T StackSize,
    SIZE_T MaximumStackSize,
    PVOID AttributeList);
extern "C" NTSYSAPI NTSTATUS NTAPI NtTerminateThread(
    HANDLE ThreadHandle,
    NTSTATUS ExitStatus);

// Thread proc for NtQueryObject invocation (must match PTHREAD_START_ROUTINE on x86).
static NTSTATUS WINAPI NtQueryObjectThreadProc(LPVOID lpThreadParameter)
{
    if (lpThreadParameter == nullptr)
    {
        return STATUS_INVALID_PARAMETER;
    }
    const NtQueryObjectParameters* ntqoParams = static_cast<const NtQueryObjectParameters*>(lpThreadParameter);
    return NtQueryObject(
        ntqoParams->Handle,
        ntqoParams->ObjectInformationClass,
        ntqoParams->ObjectInformation,
        ntqoParams->ObjectInformationLength,
        ntqoParams->ReturnLength);
}

// Implementation of the NtQueryObjectWithTimeout function.
extern "C" PSADT_NATIVE_API NTSTATUS __stdcall NtQueryObjectWithTimeout(
    HANDLE Handle,
    OBJECT_INFORMATION_CLASS ObjectInformationClass,
    PVOID ObjectInformation,
    ULONG ObjectInformationLength,
    PULONG ReturnLength,
    ULONG TimeoutMilliseconds,
    NTSTATUS* ThreadExitStatus)
{
    // Validate parameters before proceeding.
    if (Handle == nullptr || ObjectInformation == nullptr || ObjectInformationLength == 0)
    {
        return STATUS_INVALID_PARAMETER;
    }

    // Set up the thread delegate to call NtQueryObject with the provided parameters.
    NtQueryObjectParameters delegateParameters
    {
        Handle,
        ObjectInformationClass,
        ObjectInformation,
        ObjectInformationLength,
        ReturnLength,
    };

    // Create the thread and return its value if we failed.
    HANDLE threadHandle = nullptr;
    NTSTATUS createStatus = NtCreateThreadEx(
        &threadHandle,
        THREAD_ALL_ACCESS,
        nullptr,
        GetCurrentProcess(),
        NtQueryObjectThreadProc,
        &delegateParameters,
        0,
        0,
        0,
        0,
        nullptr);
    if (createStatus < 0)
    {
        return createStatus;
    }

    // Wait for the thread to complete or timeout, and handle the result accordingly.
    const DWORD waitResult = WaitForSingleObject(threadHandle, TimeoutMilliseconds);
    if (waitResult == WAIT_TIMEOUT)
    {
        NtTerminateThread(threadHandle, STATUS_TIMEOUT);
    }
    else if (waitResult == WAIT_FAILED)
    {
        CloseHandle(threadHandle);
        return STATUS_UNSUCCESSFUL;
    }

    // Retrieve the thread's exit code and return it if requested.
    DWORD exitCode = 0;
    if (!GetExitCodeThread(threadHandle, &exitCode))
    {
        CloseHandle(threadHandle);
        return STATUS_UNSUCCESSFUL;
    }
    if (ThreadExitStatus != nullptr)
    {
        *ThreadExitStatus = static_cast<NTSTATUS>(exitCode);
    }
    CloseHandle(threadHandle);
    return STATUS_SUCCESS;
}
