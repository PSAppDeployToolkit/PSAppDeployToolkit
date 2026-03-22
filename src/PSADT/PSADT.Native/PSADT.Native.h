#pragma once

#include <cstdint>
#include <ntstatus.h>
#include <winternl.h>

#if defined(PSADT_NATIVE_EXPORTS)
#define PSADT_NATIVE_API __declspec(dllexport)
#else
#define PSADT_NATIVE_API __declspec(dllimport)
#endif

extern "C"
{
    // Creates a native thread that calls NtQueryObject(ObjectNameInformation) for the specified handle,
    // waits up to the supplied timeout, and terminates the thread on timeout. Returns an NTSTATUS value for the operation itself.
    // Writes the thread exit status (NtQueryObject result or STATUS_TIMEOUT) to threadExitStatus when provided.
    PSADT_NATIVE_API NTSTATUS __stdcall NtQueryObjectWithTimeout(
        HANDLE Handle,
        OBJECT_INFORMATION_CLASS ObjectInformationClass,
        PVOID ObjectInformation,
        ULONG ObjectInformationLength,
        PULONG ReturnLength,
        ULONG TimeoutMilliseconds,
        NTSTATUS* ThreadExitStatus);
}
