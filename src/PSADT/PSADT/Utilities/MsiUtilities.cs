using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32;
using Windows.Win32.System.LibraryLoader;

namespace PSADT.Utilities
{
    /// <summary>
    /// Public P/Invokes from the msi.dll library.
    /// </summary>
    public static class MsiUtilities
    {
        /// <summary>
        /// Retrieves the message string associated with an MSI exit code from the msimsg.dll resource.
        /// </summary>
        /// <param name="msiExitCode">The MSI exit code.</param>
        /// <returns>The message string associated with the given MSI exit code.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the library cannot be loaded or the message cannot be retrieved.</exception>
        public static string? GetMessageFromMsiExitCode(uint msiExitCode)
        {
            using FreeLibrarySafeHandle hMsiMsgDll = Kernel32.LoadLibraryEx("msimsg.dll", LOAD_LIBRARY_FLAGS.LOAD_LIBRARY_AS_DATAFILE);
            Span<char> bufspan = stackalloc char[4096];
            int len = User32.LoadString(hMsiMsgDll, msiExitCode, bufspan);
            string msiMsgString = bufspan[..(len + 1)].ToString().TrimRemoveNull();
            return !string.IsNullOrWhiteSpace(msiMsgString) ? Regex.Replace(msiMsgString, @"\s{2,}", " ") : null;
        }

        /// <summary>
        /// Retrieves the list of product codes supported by the specified Windows Installer patch package (MSP file).
        /// </summary>
        /// <remarks>The returned product codes indicate which products the patch can be applied to. This
        /// method does not validate the existence or format of the specified file; callers should ensure that the path
        /// points to a valid MSP file.</remarks>
        /// <param name="szDatabasePath">The file path to the patch package (MSP) database. Cannot be null or empty.</param>
        /// <returns>A string containing the supported product codes, separated by semicolons. Returns an empty string if no
        /// product codes are found.</returns>
        public static IReadOnlyList<Guid> GetMspSupportedProductCodes(string szDatabasePath)
        {
            // Open the patch file as a database.
            Msi.MsiOpenDatabase(szDatabasePath, MSI_PERSISTENCE_MODE.MSIDBOPEN_PATCHFILE, out MsiCloseHandleSafeHandle hDatabase);
            using (hDatabase)
            {
                // Get the summary information from the database.
                Msi.MsiGetSummaryInformation(szDatabasePath, 0, out MsiCloseHandleSafeHandle hSummaryInfo);
                using (hSummaryInfo)
                {
                    // Determine the size of the buffer we need.
                    Msi.MsiSummaryInfoGetProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TEMPLATE, out _, out _, out _, null, out uint requiredSize);
                    Span<char> bufSpan = stackalloc char[(int)requiredSize];

                    // Grab the supported product codes and return them to the caller.
                    Msi.MsiSummaryInfoGetProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TEMPLATE, out _, out _, out _, bufSpan, out _);
                    return new ReadOnlyCollection<Guid>([.. bufSpan.ToString().TrimRemoveNull().Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(static g => new Guid(g))]);
                }
            }
        }

        /// <summary>
        /// Extracts and loads the XML data embedded in a Windows Installer patch file into an XmlDocument.
        /// </summary>
        /// <remarks>This method reads the XML data stored within a Windows Installer patch file using the
        /// MsiExtractPatchXMLData API. The returned XmlDocument represents the patch's internal XML metadata, which may
        /// include information about the patch contents, target products, and other installer details. The caller is
        /// responsible for handling any exceptions that may occur if the file is invalid or does not contain XML
        /// data.</remarks>
        /// <param name="szPatchPath">The full path to the patch file (.msp) from which to extract XML data. Must not be null or empty.</param>
        /// <returns>An XmlDocument containing the XML data extracted from the specified patch file.</returns>
        public static XmlDocument ExtractPatchXmlData(string szPatchPath)
        {
            Msi.MsiExtractPatchXMLData(szPatchPath, null, out uint requiredLength);
            Span<char> bufSpan = stackalloc char[(int)requiredLength];
            Msi.MsiExtractPatchXMLData(szPatchPath, bufSpan, out _);
            return XmlUtilities.SafeLoadFromText(bufSpan);
        }
    }
}
