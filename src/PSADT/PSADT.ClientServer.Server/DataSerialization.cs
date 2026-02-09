using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using PSADT.ClientServer.Payloads;
using PSADT.Foundation;
using PSADT.LibraryInterfaces;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.WindowManagement;

namespace PSADT.ClientServer
{
    /// <summary>
    /// Provides utility methods for XML serialization and deserialization using DataContractSerializer,
    /// including Base64 encoding and decoding.
    /// </summary>
    /// <remarks>The <see cref="DataSerialization"/> class offers methods to serialize objects to XML
    /// byte arrays and deserialize them back into objects. It uses <see cref="DataContractSerializer"/>
    /// with known types for secure polymorphic serialization without type name embedding.
    /// </remarks>
    public static class DataSerialization
    {
        /// <summary>
        /// Serializes the specified object directly to a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be null.</param>
        /// <returns>A byte array containing the binary XML representation of the object.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="obj"/> is null.</exception>
        /// <exception cref="SerializationException">Thrown if serialization fails.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static byte[] SerializeToBytes<T>(T obj)
        {
            if (obj is null || (obj is string str && string.IsNullOrWhiteSpace(str)))
            {
                throw new ArgumentNullException(nameof(obj), "Object to serialize cannot be null.");
            }
            using MemoryStream ms = new();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms))
            {
                GetSerializer(typeof(T)).WriteObject(writer, obj);
            }
            if (ms.ToArray() is not { Length: > 0 } result)
            {
                throw new SerializationException("Serialization returned an empty result.");
            }
            return result;
        }

        /// <summary>
        /// Deserializes the specified byte array to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="bytes">A byte array containing the binary XML to deserialize. Cannot be null or empty.</param>
        /// <returns>An instance of type T deserialized from the specified bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if deserialization fails or results in a null object.</exception>
        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            return (T)DeserializeFromBytes(bytes, 0, typeof(T));
        }

        /// <summary>
        /// Serializes the specified object to XML and encodes it as a Base64 string.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the XML representation of the specified object.</returns>
        public static string SerializeToString<T>(T obj)
        {
            return Convert.ToBase64String(SerializeToBytes(obj));
        }

        /// <summary>
        /// Deserializes a Base64-encoded XML string into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="base64Xml">The Base64-encoded XML string to deserialize. Cannot be null or empty.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the provided string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Xml"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if the deserialization process results in a null object.</exception>
        public static T DeserializeFromString<T>(string base64Xml)
        {
            return DeserializeFromBytes<T>(Convert.FromBase64String(base64Xml));
        }

        /// <summary>
        /// Deserializes an object from a byte array using the specified target type.
        /// </summary>
        /// <param name="bytes">The byte array containing the serialized data to deserialize. Cannot be null.</param>
        /// <param name="type">The type of the object to deserialize from the byte array. Cannot be null.</param>
        /// <returns>An object instance of the specified type reconstructed from the provided byte array.</returns>
        public static object DeserializeFromBytes(byte[] bytes, Type type)
        {
            return DeserializeFromBytes(bytes, 0, type);
        }

        /// <summary>
        /// Deserializes a Base64-encoded XML string into an object.
        /// </summary>
        /// <param name="base64Xml">The Base64-encoded XML string to deserialize. This parameter cannot be null or empty.</param>
        /// <param name="type">The <see cref="Type"/> to deserialize the XML into. This parameter cannot be null.</param>
        /// <returns>An object representing the deserialized data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Xml"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if the deserialization process results in a null object.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        public static object DeserializeFromString(string base64Xml, Type type)
        {
            return DeserializeFromBytes(Convert.FromBase64String(base64Xml), 0, type);
        }

        /// <summary>
        /// Deserializes the specified byte array to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="bytes">A byte array containing the binary XML to deserialize. Cannot be null or empty.</param>
        /// <param name="offset">An optional offset in the byte array to start deserialization from. Default is 0.</param>
        /// <returns>An instance of type T deserialized from the specified bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if deserialization fails or results in a null object.</exception>
        internal static T DeserializeFromBytes<T>(byte[] bytes, int offset = 0)
        {
            return (T)DeserializeFromBytes(bytes, offset, typeof(T));
        }

        /// <summary>
        /// Deserializes a value from a byte array into an object of the specified type.
        /// </summary>
        /// <param name="bytes">The byte array containing the data to deserialize. Cannot be null or empty.</param>
        /// <param name="offset">The zero-based index in the array at which to begin reading the data.</param>
        /// <param name="type">The type of the object to deserialize the data into. Cannot be null.</param>
        /// <returns>An object representing the deserialized data, cast to the specified type.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if offset is less than 0.</exception>
        /// <exception cref="ArgumentNullException">Thrown if bytes is null or if the length of bytes is less than or equal to offset.</exception>
        /// <exception cref="SerializationException">Thrown if deserialization returns a null result.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        private static object DeserializeFromBytes(byte[] bytes, int offset, Type type)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }
            if ((uint)offset > (uint)bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (offset == bytes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset points past the end of the buffer.");
            }
            using MemoryStream ms = new(bytes, offset, bytes.Length - offset, false);
            bool deserializingException = typeof(Exception).IsAssignableFrom(type);
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
            if (GetSerializer(type).ReadObject(reader, verifyObjectName: !deserializingException) is not object result)
            {
                throw new SerializationException("Deserialization returned a null result.");
            }
            if (deserializingException && result is not Exception)
            {
                throw new SerializationException($"Deserialization expected an Exception type but got {result.GetType().FullName}.");
            }
            return result;
        }

        /// <summary>
        /// Gets or creates a DataContractSerializer for the specified type with all known types.
        /// </summary>
        /// <param name="type">The type to create a serializer for.</param>
        /// <returns>A DataContractSerializer configured with all known types.</returns>
        private static DataContractSerializer GetSerializer(Type type)
        {
            return new(type, DataContractSerializerSettings);
        }

        /// <summary>
        /// Provides the default settings for the DataContractSerializer used to serialize and deserialize known
        /// payload, dialog, process, and exception types.
        /// </summary>
        /// <remarks>The settings specify a custom data contract resolver for cross-runtime compatibility,
        /// enable serialization of read-only types, and define a comprehensive list of known types including various
        /// payloads, dialog options, process information, and exception types. Object reference preservation is
        /// disabled to optimize serialization for stateless data exchange scenarios.</remarks>
        private static readonly DataContractSerializerSettings DataContractSerializerSettings = new()
        {
            PreserveObjectReferences = false,
            SerializeReadOnlyTypes = true,
            KnownTypes =
            [
                // Exception types - System namespace (core)
                typeof(Exception),
                typeof(SystemException),
                typeof(ApplicationException),
                typeof(AccessViolationException),
                typeof(AggregateException),
                typeof(AppDomainUnloadedException),
                typeof(ArgumentException),
                typeof(ArgumentNullException),
                typeof(ArgumentOutOfRangeException),
                typeof(ArithmeticException),
                typeof(ArrayTypeMismatchException),
                typeof(BadImageFormatException),
                typeof(CannotUnloadAppDomainException),
                typeof(ContextMarshalException),
                typeof(DataMisalignedException),
                typeof(DivideByZeroException),
                typeof(DllNotFoundException),
                typeof(DuplicateWaitObjectException),
                typeof(EntryPointNotFoundException),
                typeof(FieldAccessException),
                typeof(FormatException),
                typeof(IndexOutOfRangeException),
                typeof(InsufficientExecutionStackException),
                typeof(InsufficientMemoryException),
                typeof(InvalidCastException),
                typeof(InvalidOperationException),
                typeof(InvalidProgramException),
                typeof(InvalidTimeZoneException),
                typeof(MemberAccessException),
                typeof(MethodAccessException),
                typeof(MissingFieldException),
                typeof(MissingMemberException),
                typeof(MissingMethodException),
                typeof(MulticastNotSupportedException),
                typeof(NotFiniteNumberException),
                typeof(NotImplementedException),
                typeof(NotSupportedException),
                typeof(NullReferenceException),
                typeof(ObjectDisposedException),
                typeof(OperationCanceledException),
                typeof(OutOfMemoryException),
                typeof(OverflowException),
                typeof(PlatformNotSupportedException),
                typeof(RankException),
                typeof(StackOverflowException),
                typeof(TimeoutException),
                typeof(TimeZoneNotFoundException),
                typeof(TypeAccessException),
                typeof(TypeInitializationException),
                typeof(TypeLoadException),
                typeof(TypeUnloadedException),
                typeof(UnauthorizedAccessException),
                typeof(UriFormatException),
                typeof(System.Collections.Generic.KeyNotFoundException),
                typeof(System.ComponentModel.InvalidAsynchronousStateException),
                typeof(System.ComponentModel.InvalidEnumArgumentException),
                typeof(System.ComponentModel.LicenseException),
                typeof(System.ComponentModel.WarningException),
                typeof(System.ComponentModel.Win32Exception),
                typeof(System.Configuration.ConfigurationException),
                typeof(System.Data.ConstraintException),
                typeof(System.Data.DataException),
                typeof(System.Data.DBConcurrencyException),
                typeof(System.Data.DeletedRowInaccessibleException),
                typeof(System.Data.DuplicateNameException),
                typeof(System.Data.EvaluateException),
                typeof(System.Data.InRowChangingEventException),
                typeof(System.Data.InvalidConstraintException),
                typeof(System.Data.InvalidExpressionException),
                typeof(System.Data.MissingPrimaryKeyException),
                typeof(System.Data.NoNullAllowedException),
                typeof(System.Data.ReadOnlyException),
                typeof(System.Data.RowNotInTableException),
                typeof(System.Data.StrongTypingException),
                typeof(System.Data.SyntaxErrorException),
                typeof(System.Data.VersionNotFoundException),
                typeof(System.Data.Common.DbException),
                typeof(System.Globalization.CultureNotFoundException),
                typeof(DirectoryNotFoundException),
                typeof(DriveNotFoundException),
                typeof(EndOfStreamException),
                typeof(FileLoadException),
                typeof(FileNotFoundException),
                typeof(InternalBufferOverflowException),
                typeof(InvalidDataException),
                typeof(IOException),
                typeof(PathTooLongException),
                typeof(System.IO.IsolatedStorage.IsolatedStorageException),
                typeof(System.Net.CookieException),
                typeof(System.Net.HttpListenerException),
                typeof(System.Net.ProtocolViolationException),
                typeof(System.Net.WebException),
                typeof(System.Net.Mail.SmtpException),
                typeof(System.Net.Mail.SmtpFailedRecipientException),
                typeof(System.Net.Mail.SmtpFailedRecipientsException),
                typeof(System.Net.NetworkInformation.NetworkInformationException),
                typeof(System.Net.NetworkInformation.PingException),
                typeof(System.Net.Sockets.SocketException),
                typeof(System.Net.WebSockets.WebSocketException),
                typeof(System.Reflection.AmbiguousMatchException),
                typeof(System.Reflection.CustomAttributeFormatException),
                typeof(System.Reflection.InvalidFilterCriteriaException),
                typeof(System.Reflection.ReflectionTypeLoadException),
                typeof(System.Reflection.TargetException),
                typeof(System.Reflection.TargetInvocationException),
                typeof(System.Reflection.TargetParameterCountException),
                typeof(System.Resources.MissingManifestResourceException),
                typeof(System.Resources.MissingSatelliteAssemblyException),
                typeof(System.Runtime.InteropServices.COMException),
                typeof(System.Runtime.InteropServices.ExternalException),
                typeof(System.Runtime.InteropServices.InvalidComObjectException),
                typeof(System.Runtime.InteropServices.InvalidOleVariantTypeException),
                typeof(System.Runtime.InteropServices.MarshalDirectiveException),
                typeof(System.Runtime.InteropServices.SafeArrayRankMismatchException),
                typeof(System.Runtime.InteropServices.SafeArrayTypeMismatchException),
                typeof(System.Runtime.InteropServices.SEHException),
                typeof(SerializationException),
                typeof(InvalidDataContractException),
                typeof(System.Security.SecurityException),
                typeof(System.Security.VerificationException),
                typeof(System.Security.AccessControl.PrivilegeNotHeldException),
                typeof(System.Security.Authentication.AuthenticationException),
                typeof(System.Security.Authentication.InvalidCredentialException),
                typeof(System.Security.Cryptography.CryptographicException),
                typeof(System.Security.Cryptography.CryptographicUnexpectedOperationException),
                typeof(System.Security.Policy.PolicyException),
                typeof(System.Text.DecoderFallbackException),
                typeof(System.Text.EncoderFallbackException),
                typeof(System.Text.RegularExpressions.RegexMatchTimeoutException),
                typeof(System.Threading.AbandonedMutexException),
                typeof(System.Threading.BarrierPostPhaseException),
                typeof(System.Threading.LockRecursionException),
                typeof(System.Threading.SemaphoreFullException),
                typeof(System.Threading.SynchronizationLockException),
                typeof(System.Threading.ThreadInterruptedException),
                typeof(System.Threading.ThreadStateException),
                typeof(System.Threading.WaitHandleCannotBeOpenedException),
                typeof(System.Threading.Tasks.TaskCanceledException),
                typeof(System.Threading.Tasks.TaskSchedulerException),
                typeof(System.Diagnostics.Tracing.EventSourceException),
                typeof(System.Runtime.CompilerServices.RuntimeWrappedException),
                typeof(XmlException),
                typeof(System.Xml.Schema.XmlSchemaException),
                typeof(System.Xml.Schema.XmlSchemaInferenceException),
                typeof(System.Xml.Schema.XmlSchemaValidationException),
                typeof(System.Xml.XPath.XPathException),
                typeof(System.Xml.Xsl.XsltCompileException),
                typeof(System.Xml.Xsl.XsltException),

                // PSADT custom exceptions
                typeof(ClientException),
                typeof(ServerException),

                // Payload types
                typeof(EnvironmentVariablePayload),
                typeof(GetProcessWindowInfoPayload),
                typeof(GroupPolicyUpdatePayload),
                typeof(InitCloseAppsDialogPayload),
                typeof(LogMessagePayload),
                typeof(PromptToCloseAppsPayload),
                typeof(SendKeysPayload),
                typeof(ShowBalloonTipPayload),
                typeof(ShowModalDialogPayload),
                typeof(ShowProgressDialogPayload),
                typeof(UpdateProgressDialogPayload),

                // Dialog options types
                typeof(BalloonTipOptions),
                typeof(CloseAppsDialogOptions),
                typeof(CloseAppsDialogOptions.CloseAppsDialogStrings),
                typeof(CloseAppsDialogOptions.CloseAppsDialogStrings.CloseAppsDialogClassicStrings),
                typeof(CloseAppsDialogOptions.CloseAppsDialogStrings.CloseAppsDialogFluentStrings),
                typeof(CustomDialogOptions),
                typeof(DialogBoxOptions),
                typeof(HelpConsoleOptions),
                typeof(InputDialogOptions),
                typeof(ProgressDialogOptions),
                typeof(RestartDialogOptions),
                typeof(RestartDialogOptions.RestartDialogStrings),

                // Dialog result types
                typeof(CloseAppsDialogResult),
                typeof(DialogBoxResult),
                typeof(InputDialogResult),

                // Process and window types
                typeof(ProcessDefinition),
                typeof(ProcessLaunchInfo),
                typeof(ProcessResult),
                typeof(QUERY_USER_NOTIFICATION_STATE),
                typeof(RunAsActiveUser),
                typeof(WindowInfo),
                typeof(WindowInfoOptions),

                // Generic collection types - required for proper deserialization
                // DataContractSerializer needs concrete types, not interfaces
                typeof(ReadOnlyCollection<string>),
                typeof(ReadOnlyCollection<int>),
                typeof(ReadOnlyCollection<long>),
                typeof(ReadOnlyCollection<Hashtable>),
                typeof(ReadOnlyCollection<ProcessDefinition>),
            ]
        };
    }
}
