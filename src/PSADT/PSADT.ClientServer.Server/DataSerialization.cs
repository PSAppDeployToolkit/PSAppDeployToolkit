using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3236:Caller information arguments should not be provided explicitly", Justification = "This is deliberate.")]
        public static byte[] SerializeToBytes<T>(T obj)
        {
            ArgumentNullException.ThrowIfNull(obj);
            if (obj is string str)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(str, nameof(obj));
            }
            using MemoryStream ms = new();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms))
            {
                GetSerializer(typeof(T)).WriteObject(writer, obj);
            }
            return ms.ToArray() is not { Length: > 0 } result
                ? throw new SerializationException("Serialization returned an empty result.")
                : result;
        }

        /// <summary>
        /// Serializes the specified object to XML and encodes it as a Base64 string.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize. Cannot be <see langword="null"/>.</param>
        /// <returns>A Base64-encoded string containing the XML representation of the specified object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SerializeToString<T>(T obj)
        {
            return Convert.ToBase64String(SerializeToBytes(obj));
        }

        /// <summary>
        /// Deserializes the specified byte array to an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="bytes">A byte array containing the binary XML to deserialize. Cannot be null or empty.</param>
        /// <returns>An instance of type T deserialized from the specified bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="bytes"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if deserialization fails or results in a null object.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3254:Default parameter values should not be passed as arguments", Justification = "PowerShell doesn't like default parameters.")]
        public static T DeserializeFromBytes<T>(byte[] bytes)
        {
            return DeserializeFromBytes<T>(bytes, 0);
        }

        /// <summary>
        /// Deserializes an object from a byte array using the specified target type.
        /// </summary>
        /// <param name="bytes">The byte array containing the serialized data to deserialize. Cannot be null.</param>
        /// <param name="type">The type of the object to deserialize from the byte array. Cannot be null.</param>
        /// <returns>An object instance of the specified type reconstructed from the provided byte array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object DeserializeFromBytes(byte[] bytes, Type type)
        {
            return DeserializeFromBytes(bytes, 0, type);
        }

        /// <summary>
        /// Deserializes a Base64-encoded XML string into an object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="base64Xml">The Base64-encoded XML string to deserialize. Cannot be null or empty.</param>
        /// <returns>An object of type <typeparamref name="T"/> deserialized from the provided string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Xml"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if the deserialization process results in a null object.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeserializeFromString<T>(string base64Xml)
        {
            return DeserializeFromBytes<T>(Convert.FromBase64String(base64Xml));
        }

        /// <summary>
        /// Deserializes a Base64-encoded XML string into an object.
        /// </summary>
        /// <param name="base64Xml">The Base64-encoded XML string to deserialize. This parameter cannot be null or empty.</param>
        /// <param name="type">The <see cref="Type"/> to deserialize the XML into. This parameter cannot be null.</param>
        /// <returns>An object representing the deserialized data.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Xml"/> is null or empty.</exception>
        /// <exception cref="SerializationException">Thrown if the deserialization process results in a null object.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T DeserializeFromBytes<T>(byte[] bytes, int offset)
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
        private static object DeserializeFromBytes(byte[] bytes, int offset, Type type)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            ArgumentOutOfRangeException.ThrowIfZero(bytes.Length);
            if (((uint)offset > (uint)bytes.Length) || (offset == bytes.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset points past the end of the buffer.");
            }
            bool deserializingException = typeof(Exception).IsAssignableFrom(type);
            using MemoryStream ms = new(bytes, offset, bytes.Length - offset, false);
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
            return GetSerializer(type).ReadObject(reader, verifyObjectName: !deserializingException) is not object result
                ? throw new SerializationException("Deserialization returned a null result.")
                : deserializingException && result is not Exception
                ? throw new SerializationException($"Deserialization expected an Exception type but got {result.GetType().FullName}.")
                : result;
        }

        /// <summary>
        /// Gets or creates a DataContractSerializer for the specified type with all known types.
        /// </summary>
        /// <param name="type">The type to create a serializer for.</param>
        /// <returns>A DataContractSerializer configured with all known types.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            DataContractResolver = new DictionaryDataContractResolver(),
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
                typeof(RuntimeWrappedException),
                typeof(XmlException),
                typeof(System.Xml.Schema.XmlSchemaException),
                typeof(System.Xml.Schema.XmlSchemaInferenceException),
                typeof(System.Xml.Schema.XmlSchemaValidationException),
                typeof(System.Xml.XPath.XPathException),
                typeof(System.Xml.Xsl.XsltCompileException),
                typeof(System.Xml.Xsl.XsltException),

                // PSADT custom exceptions
                typeof(Interop.Exceptions.NtStatusException),
                typeof(ClientException),
                typeof(ServerException),

                // Payload types
                typeof(Payloads.EnvironmentVariablePayload),
                typeof(Payloads.GetProcessWindowInfoPayload),
                typeof(Payloads.GroupPolicyUpdatePayload),
                typeof(Payloads.InitCloseAppsDialogPayload),
                typeof(Payloads.LogMessagePayload),
                typeof(Payloads.PromptToCloseAppsPayload),
                typeof(Payloads.SendKeysPayload),
                typeof(Payloads.ShellExecuteProcessPayload),
                typeof(Payloads.ShowBalloonTipPayload),
                typeof(Payloads.ShowModalDialogPayload),
                typeof(Payloads.ShowProgressDialogPayload),
                typeof(Payloads.UpdateProgressDialogPayload),

                // Dialog options types
                typeof(UserInterface.DialogOptions.BalloonTipOptions),
                typeof(UserInterface.DialogOptions.CloseAppsDialogOptions),
                typeof(UserInterface.DialogOptions.CloseAppsDialogOptions.CloseAppsDialogStrings),
                typeof(UserInterface.DialogOptions.CloseAppsDialogOptions.CloseAppsDialogStrings.CloseAppsDialogClassicStrings),
                typeof(UserInterface.DialogOptions.CloseAppsDialogOptions.CloseAppsDialogStrings.CloseAppsDialogFluentStrings),
                typeof(UserInterface.DialogOptions.CustomDialogOptions),
                typeof(UserInterface.DialogOptions.DialogBoxOptions),
                typeof(UserInterface.DialogOptions.HelpConsoleOptions),
                typeof(UserInterface.DialogOptions.InputDialogOptions),
                typeof(UserInterface.DialogOptions.ListSelectionDialogOptions),
                typeof(UserInterface.DialogOptions.ListSelectionDialogOptions.ListSelectionDialogStrings),
                typeof(UserInterface.DialogOptions.ProgressDialogOptions),
                typeof(UserInterface.DialogOptions.RestartDialogOptions),
                typeof(UserInterface.DialogOptions.RestartDialogOptions.RestartDialogStrings),

                // Dialog result types
                typeof(UserInterface.DialogResults.CloseAppsDialogResult),
                typeof(UserInterface.DialogResults.CustomDialogDerivative),
                typeof(UserInterface.DialogResults.CustomDialogResult),
                typeof(UserInterface.DialogResults.DialogBoxResult),
                typeof(UserInterface.DialogResults.InputDialogResult),
                typeof(UserInterface.DialogResults.ListSelectionDialogResult),

                // Process and window types
                typeof(Foundation.RunAsActiveUser),
                typeof(Interop.QUERY_USER_NOTIFICATION_STATE),
                typeof(ProcessManagement.ProcessDefinition),
                typeof(ProcessManagement.ProcessLaunchInfo),
                typeof(ProcessManagement.ProcessResult),
                typeof(ProcessManagement.UserShellExecuteOptions),
                typeof(Security.ElevatedTokenType),
                typeof(WindowManagement.WindowInfo),
                typeof(WindowManagement.WindowInfoOptions),

                // Used within the following classes:
                // * ProcessManagement.ProcessLaunchInfo
                // * ProcessManagement.ProcessResult
                // * WindowManagement.WindowInfoOptions
                // * UserInterface.DialogOptions.ListSelectionDialogOptions
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<string>),

                // Used within WindowManagement.WindowInfoOptions class.
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<ulong>),
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<long>),
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<uint>),
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<int>),

                // Used within Payloads.InitCloseAppsDialogPayload class.
                typeof(System.Collections.ObjectModel.ReadOnlyCollection<ProcessManagement.ProcessDefinition>),

                // Used within UserInterface.DialogOptions.HelpConsoleOptions class.
                typeof(System.Collections.ObjectModel.ReadOnlyDictionary<string, System.Collections.Generic.IReadOnlyDictionary<string, string>>),
                typeof(System.Collections.ObjectModel.ReadOnlyDictionary<string, string>),
            ]
        };

        /// <summary>
        /// Custom DataContractResolver that handles serialization of dictionary types that share the same
        /// data contract name (ArrayOfKeyValueOfanyTypeanyType), specifically <see cref="System.Collections.Hashtable"/> and
        /// the internal <c>System.Collections.ListDictionaryInternal</c> type used by <see cref="Exception.Data"/>.
        /// </summary>
        /// <remarks>
        /// Both <see cref="System.Collections.Hashtable"/> and <c>ListDictionaryInternal</c> serialize to the same data contract name,
        /// which prevents them from being in the KnownTypes list simultaneously. This resolver handles them
        /// dynamically: serialization preserves the original type identity, while deserialization always
        /// returns <see cref="System.Collections.Hashtable"/> as the more general and publicly accessible type.
        /// </remarks>
        private sealed class DictionaryDataContractResolver : DataContractResolver
        {
            /// <summary>
            /// Maps a type to its data contract name during serialization.
            /// </summary>
            public override bool TryResolveType(Type type, Type? declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString? typeName, out XmlDictionaryString? typeNamespace)
            {
                // Handle ListDictionaryInternal and Hashtable - they both map to the same contract.
                if (type == ListDictionaryInternalType || type == typeof(System.Collections.Hashtable))
                {
                    XmlDictionary dictionary = new(2);
                    typeName = dictionary.Add(DictionaryTypeName);
                    typeNamespace = dictionary.Add(ArraysNamespace);
                    return true;
                }

                // For other types, defer to the known type resolver.
                return knownTypeResolver.TryResolveType(type, declaredType, NullContractResolver, out typeName, out typeNamespace);
            }

            /// <summary>
            /// Maps a data contract name back to a type during deserialization.
            /// </summary>
            public override Type? ResolveName(string typeName, string? typeNamespace, Type? declaredType, DataContractResolver knownTypeResolver)
            {
                // When deserializing the dictionary contract, return Hashtable (more general and public).
                if (typeName == DictionaryTypeName && typeNamespace == ArraysNamespace)
                {
                    return typeof(System.Collections.Hashtable);
                }

                // For other types, defer to the known type resolver.
                return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, NullContractResolver);
            }

            /// <summary>
            /// Represents the type object for the internal ListDictionary implementation in the System.Collections
            /// namespace, if available.
            /// </summary>
            /// <remarks>This field is initialized using reflection to avoid a direct dependency on
            /// the internal ListDictionary type. The value will be null if the type cannot be found in the current
            /// runtime environment.</remarks>
            private static readonly Type? ListDictionaryInternalType = Type.GetType("System.Collections.ListDictionaryInternal");

            /// <summary>
            /// Represents a null instance of the DataContractResolver used as a default value.
            /// </summary>
            /// <remarks>This field can be used to indicate the absence of a custom
            /// DataContractResolver when serializing or deserializing data contracts.</remarks>
            private const DataContractResolver NullContractResolver = null;

            /// <summary>
            /// Represents the XML namespace URI used for serializing arrays according to Microsoft's 2003 schema.
            /// </summary>
            private const string ArraysNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";

            /// <summary>
            /// Represents the type name for a dictionary that maps keys of any type to values of any type.
            /// </summary>
            private const string DictionaryTypeName = "ArrayOfKeyValueOfanyTypeanyType";
        }
    }
}
