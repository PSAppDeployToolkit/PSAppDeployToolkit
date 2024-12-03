using System;

namespace PSADT.Trust
{
    /// <summary>
    /// Class responsible for verifying digital signatures of files.
    /// </summary>
    public class DigitalSignature : IDisposable
    {
        internal readonly CatalogSignature _catalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalSignature"/> class.
        /// </summary>
        public DigitalSignature()
        {
            _catalog = new CatalogSignature();
        }

        /// <summary>
        /// Verifies the digital signature of the specified file.
        /// </summary>
        /// <param name="filePath">The file path of the file to verify.</param>
        /// <returns>A tuple containing the results of the Authenticode and Catalog signature checks.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="filePath"/> is null or empty.</exception>
        public SignatureVerification Verify(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path must be provided", nameof(filePath));

            bool isAuthenticodeSigned = Authenticode.Verify(filePath);
            bool isCatalogSigned = _catalog.Verify(filePath);

            return new SignatureVerification(isAuthenticodeSigned, isCatalogSigned);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="DigitalSignature"/> object.
        /// </summary>
        public void Dispose()
        {
            _catalog?.Dispose();
        }
    }
}
