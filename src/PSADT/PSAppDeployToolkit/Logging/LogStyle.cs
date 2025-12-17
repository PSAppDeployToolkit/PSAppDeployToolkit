namespace PSAppDeployToolkit.Logging
{
    /// <summary>
    /// Specifies the logging style to be used by the application.
    /// </summary>
    /// <remarks>This enumeration defines the available logging styles, which determine the format and behavior of log output.</remarks>
    public enum LogStyle
    {
        /// <summary>
        /// The log format uses PSAppDeployToolkit's legacy logging style.
        /// </summary>
        Legacy,

        /// <summary>
        /// The log format uses the Configuration Manager trace logging style.
        /// </summary>
        CMTrace,
    }
}
