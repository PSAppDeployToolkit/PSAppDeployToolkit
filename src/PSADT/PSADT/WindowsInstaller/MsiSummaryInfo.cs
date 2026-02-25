using System;
using System.Collections.Generic;
using System.Text;
using PSADT.Interop;
using Windows.Win32;

namespace PSADT.WindowsInstaller
{
    /// <summary>
    /// Represents MSI (Microsoft Installer) summary information.
    /// </summary>
    public sealed record MsiSummaryInfo
    {
        /// <summary>
        /// Retrieves summary information from a Windows Installer database, optionally applying one or more transform
        /// files.
        /// </summary>
        /// <remarks>This method uses MsiUtilities.GetSummaryInformation to access the summary
        /// information. Ensure that the database file exists and is accessible, and that any specified transform files
        /// are valid and compatible with the database.</remarks>
        /// <param name="szDatabasePath">The full path to the Windows Installer database file from which to extract summary information. This
        /// parameter cannot be null or empty.</param>
        /// <param name="szTransformFiles">An optional read-only collection of transform file paths to apply to the database before retrieving summary
        /// information. If null, no transforms are applied.</param>
        /// <returns>An instance of MsiSummaryInfo containing the summary information extracted from the specified database, with
        /// any transforms applied.</returns>
        public static MsiSummaryInfo Get(string szDatabasePath, params IReadOnlyList<string>? szTransformFiles)
        {
            // Get the summary information from the given database, with any specified transform files applied.
            using MsiCloseHandleSafeHandle hSummaryInfo = MsiUtilities.GetSummaryInformation(szDatabasePath, szTransformFiles);
            return new(hSummaryInfo);
        }

        /// <summary>
        /// Initializes a new instance of the MsiSummaryInfo class using the specified summary information handle.
        /// </summary>
        /// <remarks>This constructor populates the properties of the MsiSummaryInfo instance based on the
        /// data retrieved from the provided handle. Ensure that the handle remains valid for the duration of the
        /// operation.</remarks>
        /// <param name="hSummaryInfo">A handle to the summary information from which properties such as title, author, and creation date are
        /// retrieved. The handle must be valid and properly initialized.</param>
        internal MsiSummaryInfo(MsiCloseHandleSafeHandle hSummaryInfo)
        {
            int? codePage = MsiUtilities.GetSummaryInfoIntProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_CODEPAGE);
            CodePage = codePage.HasValue && codePage.Value != 0 ? Encoding.GetEncoding(codePage.Value) : null;
            Title = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TITLE);
            Subject = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_SUBJECT);
            Author = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_AUTHOR);
            Keywords = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_KEYWORDS);
            Comments = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_COMMENTS);
            Template = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_TEMPLATE);
            LastSavedBy = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_LASTAUTHOR);
            RevisionNumber = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_REVNUMBER);
            LastPrinted = MsiUtilities.GetSummaryInfoDateProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_LASTPRINTED);
            CreateTimeDate = MsiUtilities.GetSummaryInfoDateProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_CREATE_DTM);
            LastSaveTimeDate = MsiUtilities.GetSummaryInfoDateProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_LASTSAVE_DTM);
            PageCount = MsiUtilities.GetSummaryInfoIntProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_PAGECOUNT);
            WordCount = MsiUtilities.GetSummaryInfoIntProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_WORDCOUNT);
            CharacterCount = MsiUtilities.GetSummaryInfoIntProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_CHARCOUNT);
            CreatingApplication = MsiUtilities.GetSummaryInfoStringProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_APPNAME);
            Security = MsiUtilities.GetSummaryInfoIntProperty(hSummaryInfo, MSI_PROPERTY_ID.PID_SECURITY);
        }

        /// <summary>
        /// Gets the code page of the MSI file.
        /// </summary>
        public Encoding? CodePage { get; }

        /// <summary>
        /// Gets the title of the MSI package.
        /// </summary>
        public string? Title { get; }

        /// <summary>
        /// Gets the subject of the MSI package.
        /// </summary>
        public string? Subject { get; }

        /// <summary>
        /// Gets the author of the MSI package.
        /// </summary>
        public string? Author { get; }

        /// <summary>
        /// Gets the keywords associated with the MSI package.
        /// </summary>
        public string? Keywords { get; }

        /// <summary>
        /// Gets the comments or notes about the MSI package.
        /// </summary>
        public string? Comments { get; }

        /// <summary>
        /// Gets the template of the MSI package.
        /// </summary>
        public string? Template { get; }

        /// <summary>
        /// Gets the name of the user who last saved the MSI package.
        /// </summary>
        public string? LastSavedBy { get; }

        /// <summary>
        /// Gets the revision number of the MSI package.
        /// </summary>
        public string? RevisionNumber { get; }

        /// <summary>
        /// Gets the date and time when the MSI package was last printed.
        /// </summary>
        public DateTime? LastPrinted { get; }

        /// <summary>
        /// Gets the date and time when the MSI package was created.
        /// </summary>
        public DateTime? CreateTimeDate { get; }

        /// <summary>
        /// Gets the date and time when the MSI package was last saved.
        /// </summary>
        public DateTime? LastSaveTimeDate { get; }

        /// <summary>
        /// Gets the number of pages in the MSI package.
        /// </summary>
        public int? PageCount { get; }

        /// <summary>
        /// Gets the word count of the MSI package.
        /// </summary>
        public int? WordCount { get; }

        /// <summary>
        /// Gets the character count of the MSI package.
        /// </summary>
        public int? CharacterCount { get; }

        /// <summary>
        /// Gets the application used to create the MSI package.
        /// </summary>
        public string? CreatingApplication { get; }

        /// <summary>
        /// Gets the security descriptor for the MSI package.
        /// </summary>
        public int? Security { get; }
    }
}
