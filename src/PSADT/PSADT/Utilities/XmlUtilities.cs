using System;
using System.IO;
using System.Xml;
using PSADT.Extensions;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for securely loading and parsing XML documents with protection against common XML
    /// security vulnerabilities.
    /// </summary>
    /// <remarks>All methods in this class use secure XML reader settings to help prevent XML external entity
    /// (XXE) attacks and prohibit DTD processing. The utilities are intended for scenarios where XML input may be
    /// untrusted or where external resource resolution should be disabled.</remarks>
    public static class XmlUtilities
    {
        /// <summary>
        /// Loads an XML document from the specified file path in a safe manner.
        /// </summary>
        /// <param name="path">The file system path to the XML file to load. Cannot be null or empty.</param>
        /// <returns>An XmlDocument representing the contents of the specified file.</returns>
        public static XmlDocument SafeLoadFromPath(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Cannot find path '{path}' because it does not exist.");
            }
            using StreamReader reader = new(path);
            return SafeLoadCommon(reader);
        }

        /// <summary>
        /// Loads an XML document from the specified stream in a safe manner.
        /// </summary>
        /// <param name="stream">The stream containing the XML data to load. Cannot be null.</param>
        /// <returns>An XmlDocument representing the contents of the stream.</returns>
        public static XmlDocument SafeLoadFromStream(Stream stream)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            using StreamReader reader = new(stream);
            return SafeLoadCommon(reader);
        }

        /// <summary>
        /// Parses the specified XML string and returns an XmlDocument instance representing its contents.
        /// </summary>
        /// <param name="input">A string containing the XML data to parse. Cannot be null.</param>
        /// <returns>An XmlDocument representing the parsed XML content.</returns>
        public static XmlDocument SafeLoadFromText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input));
            }
            using StringReader reader = new(input);
            return SafeLoadCommon(reader);
        }

        /// <summary>
        /// Parses the specified character span as XML and returns an XmlDocument instance representing the parsed
        /// content.
        /// </summary>
        /// <param name="input">A read-only span of characters containing the XML data to parse.</param>
        /// <returns>An XmlDocument representing the parsed XML content.</returns>
        public static XmlDocument SafeLoadFromText(ReadOnlySpan<char> input)
        {
            return SafeLoadFromText(input.ToString().TrimRemoveNull());
        }

        /// <summary>
        /// Loads an XML document from the specified text input stream using secure reader settings.
        /// </summary>
        /// <remarks>The returned <see cref="XmlDocument"/> has its <see cref="XmlDocument.XmlResolver"/>
        /// property set to <see langword="null"/> to prevent external resource resolution. The method uses secure XML
        /// reader settings to help mitigate common XML security vulnerabilities.</remarks>
        /// <param name="text">A <see cref="TextReader"/> that provides the XML data to load. The stream must be positioned at the start of
        /// the XML content and remain open for the duration of the read operation.</param>
        /// <returns>An <see cref="XmlDocument"/> containing the XML data loaded from the input stream.</returns>
        internal static XmlDocument SafeLoadCommon(TextReader text)
        {
            XmlDocument xmlDoc = new() { XmlResolver = null };
            using (XmlReader reader = XmlReader.Create(text, SafeReaderSettings))
            {
                xmlDoc.Load(reader);
            }
            return xmlDoc;
        }

        /// <summary>
        /// Provides XmlReaderSettings configured to prohibit DTD processing and prevent external resource resolution
        /// for secure XML parsing.
        /// </summary>
        /// <remarks>Use these settings to help protect against XML external entity (XXE) attacks and
        /// other security vulnerabilities when reading untrusted XML data. The settings disable DTD processing and set
        /// the XmlResolver to null, preventing the parser from accessing external resources.</remarks>
        private static readonly XmlReaderSettings SafeReaderSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
    }
}
