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
            int codePage,
            string title,
            string subject,
            string author,
            string keywords,
            string comments,
            string template,
            string lastSavedBy,
            Guid revisionNumber,
            DateTime? lastPrinted,
            DateTime createTimeDate,
            DateTime lastSaveTimeDate,
            int pageCount,
            int wordCount,
            int? characterCount,
            string creatingApplication,
            int security)
        {
            CodePage = codePage;
            Title = !string.IsNullOrWhiteSpace(title) ? title : string.Empty;
            Subject = !string.IsNullOrWhiteSpace(subject) ? subject : string.Empty;
            Author = !string.IsNullOrWhiteSpace(author) ? author : string.Empty;
            Keywords = !string.IsNullOrWhiteSpace(keywords) ? keywords : string.Empty;
            Comments = !string.IsNullOrWhiteSpace(comments) ? comments : string.Empty;
            Template = !string.IsNullOrWhiteSpace(template) ? template : string.Empty;
            LastSavedBy = !string.IsNullOrWhiteSpace(lastSavedBy) ? lastSavedBy : string.Empty;
            RevisionNumber = revisionNumber;
            LastPrinted = lastPrinted;
            CreateTimeDate = createTimeDate;
            LastSaveTimeDate = lastSaveTimeDate;
            PageCount = pageCount;
            WordCount = wordCount;
            CharacterCount = characterCount;
            CreatingApplication = !string.IsNullOrWhiteSpace(creatingApplication) ? creatingApplication : string.Empty;
            Security = security;
        }

        /// <summary>
        /// Gets the code page of the MSI file.
        /// </summary>
        public int CodePage { get; }

        /// <summary>
        /// Gets the title of the MSI package.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the subject of the MSI package.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the author of the MSI package.
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Gets the keywords associated with the MSI package.
        /// </summary>
        public string Keywords { get; }

        /// <summary>
        /// Gets the comments or notes about the MSI package.
        /// </summary>
        public string Comments { get; }

        /// <summary>
        /// Gets the template of the MSI package.
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// Gets the name of the user who last saved the MSI package.
        /// </summary>
        public string LastSavedBy { get; }

        /// <summary>
        /// Gets the revision number of the MSI package.
        /// </summary>
        public Guid RevisionNumber { get; }

        /// <summary>
        /// Gets the date and time when the MSI package was last printed.
        /// </summary>
        public DateTime? LastPrinted { get; }

        /// <summary>
        /// Gets the date and time when the MSI package was created.
        /// </summary>
        public DateTime CreateTimeDate { get; }

        /// <summary>
        /// Gets the date and time when the MSI package was last saved.
        /// </summary>
        public DateTime LastSaveTimeDate { get; }

        /// <summary>
        /// Gets the number of pages in the MSI package.
        /// </summary>
        public int PageCount { get; }

        /// <summary>
        /// Gets the word count of the MSI package.
        /// </summary>
        public int WordCount { get; }

        /// <summary>
        /// Gets the character count of the MSI package.
        /// </summary>
        public int? CharacterCount { get; }

        /// <summary>
        /// Gets the application used to create the MSI package.
        /// </summary>
        public string CreatingApplication { get; }

        /// <summary>
        /// Gets the security descriptor for the MSI package.
        /// </summary>
        public int Security { get; }
    }
}
