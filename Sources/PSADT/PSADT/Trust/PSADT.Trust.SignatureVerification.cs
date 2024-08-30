namespace PSADT.Trust
{
    public class SignatureVerification
    {
        /// <summary>
        /// Gets or sets a value indicating whether the file is Authenticode signed.
        /// </summary>
        public bool IsAuthenticodeSigned { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file is Catalog signed.
        /// </summary>
        public bool IsCatalogSigned { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignatureVerification"/> class.
        /// </summary>
        /// <param name="isAuthenticodeSigned">Indicates if the file is Authenticode signed.</param>
        /// <param name="isCatalogSigned">Indicates if the file is Catalog signed.</param>
        public SignatureVerification(bool isAuthenticodeSigned, bool isCatalogSigned)
        {
            IsAuthenticodeSigned = isAuthenticodeSigned;
            IsCatalogSigned = isCatalogSigned;
        }
    }
}
