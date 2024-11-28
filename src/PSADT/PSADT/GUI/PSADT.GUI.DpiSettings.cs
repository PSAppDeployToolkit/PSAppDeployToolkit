namespace PSADT.GUI
{
    /// <summary>
    /// Represents the DPI settings for a display or monitor.
    /// </summary>
    public class DpiSettings
    {
        /// <summary>
        /// Gets or sets the horizontal DPI.
        /// </summary>
        public int DpiX { get; set; }

        /// <summary>
        /// Gets or sets the vertical DPI.
        /// </summary>
        public int DpiY { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DpiSettings"/> class.
        /// </summary>
        /// <param name="dpiX">The horizontal DPI.</param>
        /// <param name="dpiY">The vertical DPI.</param>
        public DpiSettings(int dpiX, int dpiY)
        {
            DpiX = dpiX;
            DpiY = dpiY;
        }
    }
}
