namespace PSADT.Interop
{
    /// <summary>
    /// Specifies property identifiers used to access standard metadata properties in Windows Installer (MSI) files.
    /// </summary>
    /// <remarks>These property IDs correspond to common document metadata fields, such as title, author, and
    /// creation date, and are typically used when reading or writing summary information streams in MSI packages. The
    /// values align with standard property IDs defined by the Windows property system.</remarks>
    internal enum MSI_PROPERTY_ID : uint
    {
        /// <summary>
        /// The property ID for "Codepage".
        /// </summary>
        PID_CODEPAGE = Windows.Win32.PInvoke.PID_CODEPAGE,

        /// <summary>
        /// The property ID for "Title".
        /// </summary>
        PID_TITLE = Windows.Win32.PInvoke.PID_TITLE,

        /// <summary>
        /// The property ID for "Subject".
        /// </summary>
        PID_SUBJECT = Windows.Win32.PInvoke.PID_SUBJECT,

        /// <summary>
        /// The property ID for "Author".
        /// </summary>
        PID_AUTHOR = Windows.Win32.PInvoke.PID_AUTHOR,

        /// <summary>
        /// The property ID for "Keywords".
        /// </summary>
        PID_KEYWORDS = Windows.Win32.PInvoke.PID_KEYWORDS,

        /// <summary>
        /// The property ID for "Comments".
        /// </summary>
        PID_COMMENTS = Windows.Win32.PInvoke.PID_COMMENTS,

        /// <summary>
        /// The property ID for "Template".
        /// </summary>
        PID_TEMPLATE = Windows.Win32.PInvoke.PID_TEMPLATE,

        /// <summary>
        /// The property ID for "Last Author".
        /// </summary>
        PID_LASTAUTHOR = Windows.Win32.PInvoke.PID_LASTAUTHOR,

        /// <summary>
        /// The property ID for "Revision Number".
        /// </summary>
        PID_REVNUMBER = Windows.Win32.PInvoke.PID_REVNUMBER,

        /// <summary>
        /// The property ID for "Last Printed".
        /// </summary>
        PID_LASTPRINTED = Windows.Win32.PInvoke.PID_LASTPRINTED,

        /// <summary>
        /// The property ID for "Create Time/Date".
        /// </summary>
        PID_CREATE_DTM = Windows.Win32.PInvoke.PID_CREATE_DTM,

        /// <summary>
        /// The property ID for "Last Save Time/Date".
        /// </summary>
        PID_LASTSAVE_DTM = Windows.Win32.PInvoke.PID_LASTSAVE_DTM,

        /// <summary>
        /// The property ID for "Page Count".
        /// </summary>
        PID_PAGECOUNT = Windows.Win32.PInvoke.PID_PAGECOUNT,

        /// <summary>
        /// The property ID for "Word Count".
        /// </summary>
        PID_WORDCOUNT = Windows.Win32.PInvoke.PID_WORDCOUNT,

        /// <summary>
        /// The property ID for "Character Count".
        /// </summary>
        PID_CHARCOUNT = Windows.Win32.PInvoke.PID_CHARCOUNT,

        /// <summary>
        /// The property ID for "Creating Application".
        /// </summary>
        PID_APPNAME = Windows.Win32.PInvoke.PID_APPNAME,

        /// <summary>
        /// The property ID for "Security".
        /// </summary>
        PID_SECURITY = Windows.Win32.PInvoke.PID_SECURITY,
    }
}
