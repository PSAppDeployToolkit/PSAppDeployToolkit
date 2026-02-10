using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using PSADT.LibraryInterfaces.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.ApplicationInstallationAndServicing;
using Windows.Win32.System.Variant;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides internal helper methods and exception types for interacting with the Windows Installer (MSI) API.
    /// </summary>
    /// <remarks>This static class contains methods for formatting records, opening databases, retrieving
    /// summary information, and handling errors when working with Windows Installer APIs. It is intended for internal
    /// use to facilitate safe and consistent access to MSI functionality, including proper resource management and
    /// error handling.</remarks>
    internal static class Msi
    {
        /// <summary>
        /// Opens a Windows Installer database and returns a handle to the opened database.
        /// </summary>
        /// <remarks>The returned handle must be released by calling the appropriate close method to avoid
        /// resource leaks. If the operation fails, an exception is thrown and the out parameter is not set.</remarks>
        /// <param name="szDatabasePath">The path to the Windows Installer database file to open. Cannot be null or empty.</param>
        /// <param name="szPersist">The persistence mode to use when opening the database, specifying whether the database is opened as
        /// read-only or read/write.</param>
        /// <param name="phDatabase">When this method returns, contains a handle to the opened database. This handle must be closed by the caller
        /// when no longer needed.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns WIN32_ERROR.ERROR_SUCCESS if the
        /// database was opened successfully.</returns>
        internal static WIN32_ERROR MsiOpenDatabase(string szDatabasePath, MSI_PERSISTENCE_MODE szPersist, out MsiCloseHandleSafeHandle phDatabase)
        {
            WIN32_ERROR res;
            unsafe
            {
                MSIHANDLE phDatabaseLocal = default;
                fixed (char* pszDatabasePath = szDatabasePath)
                {
                    res = (WIN32_ERROR)PInvoke.MsiOpenDatabase(pszDatabasePath, (PCWSTR)szPersist, &phDatabaseLocal);
                }
                if (res != WIN32_ERROR.ERROR_SUCCESS)
                {
                    throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
                }
                phDatabase = new MsiCloseHandleSafeHandle(phDatabaseLocal, true);
            }
            return res;
        }

        /// <summary>
        /// Retrieves summary information from an installer database or summary information stream and returns a handle
        /// to access the summary information.
        /// </summary>
        /// <remarks>The caller is responsible for closing the returned summary information handle when it
        /// is no longer needed. If the operation fails, an exception is thrown and phSummaryInfo is not set.</remarks>
        /// <param name="hDatabase">A handle to the open installer database. If null, the function opens the database specified by
        /// szDatabasePath.</param>
        /// <param name="szDatabasePath">The path to the installer database file. Used only if hDatabase is null. Can be null if hDatabase is
        /// provided.</param>
        /// <param name="uiUpdateCount">The number of summary information properties to be updated. Set to 0 if no properties will be updated.</param>
        /// <param name="phSummaryInfo">When this method returns, contains a handle to the summary information stream. This parameter is passed
        /// uninitialized.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns ERROR_SUCCESS if the operation succeeds.</returns>
        internal static WIN32_ERROR MsiGetSummaryInformation(SafeHandle? hDatabase, string? szDatabasePath, uint uiUpdateCount, out MsiCloseHandleSafeHandle phSummaryInfo)
        {
            MSIHANDLE phSummaryInfoLocal = default;
            WIN32_ERROR res;
            if (hDatabase is null)
            {
                using SafeFileHandle nullHandle = new(default, true);
                res = (WIN32_ERROR)PInvoke.MsiGetSummaryInformation(nullHandle, szDatabasePath, uiUpdateCount, ref phSummaryInfoLocal);
            }
            else
            {
                res = (WIN32_ERROR)PInvoke.MsiGetSummaryInformation(hDatabase, szDatabasePath, uiUpdateCount, ref phSummaryInfoLocal);
            }
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            phSummaryInfo = new MsiCloseHandleSafeHandle(phSummaryInfoLocal, true);
            return res;
        }

        /// <summary>
        /// Retrieves summary information from an open Windows Installer database and returns a handle to the summary
        /// information stream.
        /// </summary>
        /// <param name="hDatabase">A handle to the open Windows Installer database. Can be null to indicate that the summary information is to
        /// be retrieved from a file specified elsewhere.</param>
        /// <param name="uiUpdateCount">The maximum number of updated properties that can be written to the summary information stream. Set to 0 to
        /// open the stream for read-only access.</param>
        /// <param name="phSummaryInfo">When this method returns, contains a handle to the summary information stream. This handle must be closed
        /// with the appropriate method when no longer needed.</param>
        /// <returns>A value of the WIN32_ERROR enumeration that indicates the result of the operation. Returns
        /// WIN32_ERROR.SUCCESS if the operation succeeds; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR MsiGetSummaryInformation(SafeHandle? hDatabase, uint uiUpdateCount, out MsiCloseHandleSafeHandle phSummaryInfo)
        {
            return MsiGetSummaryInformation(hDatabase, null, uiUpdateCount, out phSummaryInfo);
        }

        /// <summary>
        /// Retrieves summary information from a specified Windows Installer database file.
        /// </summary>
        /// <param name="szDatabasePath">The path to the Windows Installer database file. Can be null to indicate a null database handle.</param>
        /// <param name="uiUpdateCount">The number of summary information properties to be updated. Set to 0 to open the summary information in
        /// read-only mode.</param>
        /// <param name="phSummaryInfo">When this method returns, contains a handle to the summary information stream. This parameter is passed
        /// uninitialized.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns WIN32_ERROR.SUCCESS if the summary
        /// information was retrieved successfully; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR MsiGetSummaryInformation(string? szDatabasePath, uint uiUpdateCount, out MsiCloseHandleSafeHandle phSummaryInfo)
        {
            return MsiGetSummaryInformation(null, szDatabasePath, uiUpdateCount, out phSummaryInfo);
        }

        /// <summary>
        /// Retrieves a property value from the specified Windows Installer summary information stream.
        /// </summary>
        /// <remarks>The caller should examine the data type returned in puiDataType to determine which of
        /// the output parameters contains the valid property value. Only one of piValue, pftValue, or szValueBuf will
        /// be populated based on the property type. If szValueBuf is too small to hold the string value, the required
        /// buffer size is returned in pcchValueBuf.</remarks>
        /// <param name="hSummaryInfo">A handle to the summary information stream from which to retrieve the property. The handle must be valid and
        /// open for reading.</param>
        /// <param name="uiProperty">The identifier of the summary property to retrieve.</param>
        /// <param name="puiDataType">When this method returns, contains the data type of the retrieved property value.</param>
        /// <param name="piValue">When this method returns, contains the integer value of the property, if applicable. Otherwise, set to zero.</param>
        /// <param name="pftValue">When this method returns, contains the FILETIME value of the property, if applicable.</param>
        /// <param name="szValueBuf">A buffer that receives the string value of the property, if applicable. If the property is not a string,
        /// this buffer is not modified.</param>
        /// <param name="pcchValueBuf">When this method returns, contains the number of characters written to szValueBuf, or the required buffer
        /// size if szValueBuf is too small.</param>
        /// <returns>A WIN32_ERROR value indicating the result of the operation. Returns ERROR_SUCCESS if the property was
        /// retrieved successfully; otherwise, returns an error code.</returns>
        internal static WIN32_ERROR MsiSummaryInfoGetProperty(SafeHandle hSummaryInfo, MSI_PROPERTY_ID uiProperty, out VARENUM puiDataType, out int piValue, out System.Runtime.InteropServices.ComTypes.FILETIME pftValue, Span<char> szValueBuf, out uint pcchValueBuf)
        {
            uint pcchValueBufLocal = (uint)szValueBuf.Length;
            WIN32_ERROR res = (WIN32_ERROR)PInvoke.MsiSummaryInfoGetProperty(hSummaryInfo, (uint)uiProperty, out uint puiDataTypeLocal, out piValue, out pftValue, szValueBuf, ref pcchValueBufLocal);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            if (szValueBuf.IsEmpty)
            {
                pcchValueBufLocal++;
            }
            puiDataType = (VARENUM)puiDataTypeLocal;
            pcchValueBuf = pcchValueBufLocal;
            return res;
        }

        /// <summary>
        /// Extracts the XML data embedded in a Windows Installer patch file and copies it into the provided character
        /// buffer.
        /// </summary>
        /// <remarks>If the buffer specified by <paramref name="szXMLData"/> is too small to hold the XML
        /// data, the method throws an exception. Ensure that the buffer is sized appropriately before calling this
        /// method.</remarks>
        /// <param name="szPatchPath">The full path to the patch file from which to extract XML data. Cannot be null.</param>
        /// <param name="szXMLData">A buffer that receives the extracted XML data as characters. The buffer must be large enough to hold the XML
        /// data; otherwise, the operation will fail.</param>
        /// <param name="pcchXMLData">When this method returns, contains the number of characters copied to <paramref name="szXMLData"/>.</param>
        /// <returns>A <see cref="WIN32_ERROR"/> value indicating the result of the extraction operation. Returns <see
        /// cref="WIN32_ERROR.ERROR_SUCCESS"/> if the XML data was successfully extracted.</returns>
        internal static WIN32_ERROR MsiExtractPatchXMLData(string szPatchPath, Span<char> szXMLData, out uint pcchXMLData)
        {
            uint pcchXMLDataLocal = (uint)szXMLData.Length;
            WIN32_ERROR res = (WIN32_ERROR)PInvoke.MsiExtractPatchXMLData(szPatchPath, szXMLData, ref pcchXMLDataLocal);
            if (res != WIN32_ERROR.ERROR_SUCCESS)
            {
                throw ExceptionUtilities.GetExceptionForLastWin32Error(res);
            }
            if (szXMLData.IsEmpty)
            {
                pcchXMLDataLocal++;
            }
            pcchXMLData = pcchXMLDataLocal;
            return res;
        }
    }
}
