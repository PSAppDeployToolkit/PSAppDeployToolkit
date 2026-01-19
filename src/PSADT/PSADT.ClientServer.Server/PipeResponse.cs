using System;
using Newtonsoft.Json;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Represents a response sent from the client to the server through the pipe communication channel.
    /// </summary>
    /// <remarks>This class encapsulates the result of a command execution, including
    /// optional result data, and error information if the command failed.</remarks>
    internal sealed record PipeResponse
    {
        /// <summary>
        /// The result data from the command execution.
        /// </summary>
        /// <remarks>The result type varies depending on the command that was executed.
        /// For commands that don't return data, this field may be null.</remarks>
        [JsonProperty]
        internal readonly object? Result;

        /// <summary>
        /// The exception if the command failed.
        /// </summary>
        /// <remarks>This field is only populated when the command failed.</remarks>
        [JsonProperty]
        internal readonly Exception? Error;

        /// <summary>
        /// Gets a value indicating whether the command executed successfully.
        /// </summary>
        [JsonIgnore]
        internal bool Success => Error is null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipeResponse"/> class.
        /// </summary>
        /// <param name="result">The result data from the command execution.</param>
        /// <param name="error">The exception if the command failed.</param>
        [JsonConstructor]
        private PipeResponse(object? result = null, Exception? error = null)
        {
            Result = result;
            Error = error;
        }

        /// <summary>
        /// Creates a successful response with the specified result.
        /// </summary>
        /// <typeparam name="T">The type of the result data.</typeparam>
        /// <param name="result">The result data to include in the response.</param>
        /// <returns>A new <see cref="PipeResponse"/> instance indicating success.</returns>
        internal static PipeResponse Ok<T>(T result)
        {
            return new(result);
        }

        /// <summary>
        /// Creates a failure response with the specified error.
        /// </summary>
        /// <param name="error">The exception that caused the failure.</param>
        /// <returns>A new <see cref="PipeResponse"/> instance indicating failure.</returns>
        internal static PipeResponse Fail(Exception error)
        {
            return new(null, error);
        }
    }
}
