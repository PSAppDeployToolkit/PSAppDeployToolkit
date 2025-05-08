using System;

namespace PSADT.Types
{
    /// <summary>
    /// Represents MSI (Microsoft Installer) summary information.
    /// </summary>
    public sealed record MsiSummaryInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsiSummaryInfo"/> struct.
        /// </summary>
        /// <param name="codePage">The code page of the MSI file.</param>
        /// <param name="title">The title of the MSI package.</param>
        /// <param name="subject">The subject of the MSI package.</param>
        /// <param name="author">The author of the MSI package.</param>
        /// <param name="keywords">The keywords associated with the MSI package.</param>
        /// <param name="comments">The comments or notes about the MSI package.</param>
        /// <param name="template">The template of the MSI package.</param>
        /// <param name="lastSavedBy">The name of the user who last saved the MSI package.</param>
        /// <param name="revisionNumber">The revision number of the MSI package.</param>
        /// <param name="lastPrinted">The date and time when the MSI package was last printed.</param>
        /// <param name="createTimeDate">The date and time when the MSI package was created.</param>
        /// <param name="lastSaveTimeDate">The date and time when the MSI package was last saved.</param>
        /// <param name="pageCount">The number of pages in the MSI package.</param>
        /// <param name="wordCount">The word count of the MSI package.</param>
        /// <param name="characterCount">The character count of the MSI package.</param>
        /// <param name="creatingApplication">The application used to create the MSI package.</param>
        /// <param name="security">The security descriptor for the MSI package.</param>
        public MsiSummaryInfo(
            int codePage, string title, string subject, string author, string keywords,
            string comments, string template, string lastSavedBy, Guid revisionNumber,
            DateTime? lastPrinted, DateTime createTimeDate, DateTime lastSaveTimeDate,
            int pageCount, int wordCount, int? characterCount, string creatingApplication, int security)
        {
            CodePage = codePage;
            Title = title ?? string.Empty;
            Subject = subject ?? string.Empty;
            Author = author ?? string.Empty;
            Keywords = keywords ?? string.Empty;
            Comments = comments ?? string.Empty;
            Template = template ?? string.Empty;
            LastSavedBy = lastSavedBy ?? string.Empty;
            RevisionNumber = revisionNumber;
            LastPrinted = lastPrinted;
            CreateTimeDate = createTimeDate;
            LastSaveTimeDate = lastSaveTimeDate;
            PageCount = pageCount;
            WordCount = wordCount;
            CharacterCount = characterCount;
            CreatingApplication = creatingApplication ?? string.Empty;
            Security = security;
        }

        /// <summary>
        /// Gets the code page of the MSI file.
        /// </summary>
        public readonly int CodePage;

        /// <summary>
        /// Gets the title of the MSI package.
        /// </summary>
        public readonly string Title;

        /// <summary>
        /// Gets the subject of the MSI package.
        /// </summary>
        public readonly string Subject;

        /// <summary>
        /// Gets the author of the MSI package.
        /// </summary>
        public readonly string Author;

        /// <summary>
        /// Gets the keywords associated with the MSI package.
        /// </summary>
        public readonly string Keywords;

        /// <summary>
        /// Gets the comments or notes about the MSI package.
        /// </summary>
        public readonly string Comments;

        /// <summary>
        /// Gets the template of the MSI package.
        /// </summary>
        public readonly string Template;

        /// <summary>
        /// Gets the name of the user who last saved the MSI package.
        /// </summary>
        public readonly string LastSavedBy;

        /// <summary>
        /// Gets the revision number of the MSI package.
        /// </summary>
        public readonly Guid RevisionNumber;

        /// <summary>
        /// Gets the date and time when the MSI package was last printed.
        /// </summary>
        public readonly DateTime? LastPrinted;

        /// <summary>
        /// Gets the date and time when the MSI package was created.
        /// </summary>
        public readonly DateTime CreateTimeDate;

        /// <summary>
        /// Gets the date and time when the MSI package was last saved.
        /// </summary>
        public readonly DateTime LastSaveTimeDate;

        /// <summary>
        /// Gets the number of pages in the MSI package.
        /// </summary>
        public readonly int PageCount;

        /// <summary>
        /// Gets the word count of the MSI package.
        /// </summary>
        public readonly int WordCount;

        /// <summary>
        /// Gets the character count of the MSI package.
        /// </summary>
        public readonly int? CharacterCount;

        /// <summary>
        /// Gets the application used to create the MSI package.
        /// </summary>
        public readonly string CreatingApplication;

        /// <summary>
        /// Gets the security descriptor for the MSI package.
        /// </summary>
        public readonly int Security;

        /// <summary>
        /// Returns a string representation of the MSI summary information.
        /// </summary>
        /// <returns>A formatted string with the MSI summary details.</returns>
        public override string ToString()
        {
            return $"Title: {Title}, Subject: {Subject}, Author: {Author}, Revision Number: {RevisionNumber}";
        }
    }
}
