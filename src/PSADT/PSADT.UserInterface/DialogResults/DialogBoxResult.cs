using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Specifies the possible results of a message box operation.
    /// </summary>
    /// <remarks>This struct represents the various outcomes of a message box interaction, such as the button selected by the user or other conditions like a timeout. Each value corresponds to a specific Windows API constant from MESSAGEBOX_RESULT. These results are typically used to determine the user's response to a prompt or dialog.</remarks>
    [DataContract]
    public readonly struct DialogBoxResult : IDialogResult, IEquatable<DialogBoxResult>
    {
        /// <summary>
        /// Represents the result of a message box operation where the user selects the "OK" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDOK. It is typically used to indicate that the user acknowledged the message or completed an action in response to a prompt.</remarks>
        public static readonly DialogBoxResult OK = Create(MESSAGEBOX_RESULT.IDOK);

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Cancel" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDCANCEL. It is typically used to indicate that the user canceled the operation or dismissed the message box without making a selection.</remarks>
        public static readonly DialogBoxResult Cancel = Create(MESSAGEBOX_RESULT.IDCANCEL);

        /// <summary>
        /// Represents the result of a message box operation where the user selected the "Abort" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDABORT. It is typically used to indicate that the user chose to abort an operation in response to a message box prompt.</remarks>
        public static readonly DialogBoxResult Abort = Create(MESSAGEBOX_RESULT.IDABORT);

        /// <summary>
        /// Represents the result of a message box operation where the user selects the "Retry" option.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDRETRY. It is typically used to indicate that the user has chosen to retry an operation after encountering an error or prompt.</remarks>
        public static readonly DialogBoxResult Retry = Create(MESSAGEBOX_RESULT.IDRETRY);

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Ignore."
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDIGNORE. It is typically used to handle scenarios where the user chooses to ignore a warning or error.</remarks>
        public static readonly DialogBoxResult Ignore = Create(MESSAGEBOX_RESULT.IDIGNORE);

        /// <summary>
        /// Represents a result indicating that the user selected "Yes" in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDYES. It is typically used to indicate that the user has confirmed an action or answered "Yes" to a prompt.</remarks>
        public static readonly DialogBoxResult Yes = Create(MESSAGEBOX_RESULT.IDYES);

        /// <summary>
        /// Represents the result of a message box operation where the user selected "No".
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDNO. It is typically used to indicate that the user declined an action or answered "No" to a prompt.</remarks>
        public static readonly DialogBoxResult No = Create(MESSAGEBOX_RESULT.IDNO);

        /// <summary>
        /// Represents the result of a message box when the Close button is selected.
        /// </summary>
        /// <remarks>This value corresponds to the Close button being clicked or the message box being dismissed without selecting any other option. It is typically used to handle scenarios where the user closes the message box without making a specific choice.</remarks>
        public static readonly DialogBoxResult Close = Create(MESSAGEBOX_RESULT.IDCLOSE);

        /// <summary>
        /// Represents the result indicating that the user selected the "Try Again" option in a message box.
        /// </summary>
        /// <remarks>This value corresponds to the Windows API constant MESSAGEBOX_RESULT.IDTRYAGAIN. It is typically used to handle scenarios where the user opts to retry an operation after a failure.</remarks>
        public static readonly DialogBoxResult TryAgain = Create(MESSAGEBOX_RESULT.IDTRYAGAIN);

        /// <summary>
        /// Represents the result of a message box operation where the user selects "Continue."
        /// </summary>
        /// <remarks>This value corresponds to the "Continue" button in a message box, typically used to indicate that the user has chosen to proceed with the operation.</remarks>
        public static readonly DialogBoxResult Continue = Create(MESSAGEBOX_RESULT.IDCONTINUE);

        /// <summary>
        /// Represents the result of a message box operation when the operation times out.
        /// </summary>
        /// <remarks>This value is returned when a message box is displayed with a timeout and the timeout period elapses before the user interacts with the message box.</remarks>
        public static readonly DialogBoxResult Timeout = Create(MESSAGEBOX_RESULT.IDTIMEOUT);

        /// <summary>
        /// Converts a numeric value to its corresponding <see cref="DialogBoxResult"/> instance.
        /// </summary>
        /// <param name="value">The numeric value to convert.</param>
        /// <returns>The corresponding <see cref="DialogBoxResult"/> static instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value does not correspond to a known result.</exception>
        internal static DialogBoxResult FromMessageBoxResult(MESSAGEBOX_RESULT value)
        {
            return !MessageBoxResultMap.TryGetValue(value, out DialogBoxResult result)
                ? throw new ArgumentOutOfRangeException(nameof(value), value, $"Unknown DialogBoxResult value: {value}")
                : result;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DialogBoxResult"/> struct.
        /// </summary>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        /// <param name="name">The name to be associated with this instance for string comparisons. Automatically captured from the caller member name.</param>
        /// <returns>A new <see cref="DialogBoxResult"/> instance.</returns>
        private static DialogBoxResult Create(MESSAGEBOX_RESULT value, [CallerMemberName] string name = null!)
        {
            return new(name, (uint)value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogBoxResult"/> struct with the specified name and value.
        /// </summary>
        /// <param name="name">The name to be associated with this instance for string comparisons.</param>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        private DialogBoxResult(string name, uint value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Converts this instance to a signed byte value.
        /// </summary>
        /// <returns>The signed byte representation of this result's value.</returns>
        public sbyte ToSByte()
        {
            return (sbyte)Value;
        }

        /// <summary>
        /// Converts this instance to an unsigned byte value.
        /// </summary>
        /// <returns>The unsigned byte representation of this result's value.</returns>
        public byte ToByte()
        {
            return (byte)Value;
        }

        /// <summary>
        /// Converts this instance to a signed 16-bit integer value.
        /// </summary>
        /// <returns>The signed 16-bit integer representation of this result's value.</returns>
        public short ToInt16()
        {
            return (short)Value;
        }

        /// <summary>
        /// Converts this instance to an unsigned 16-bit integer value.
        /// </summary>
        /// <returns>The unsigned 16-bit integer representation of this result's value.</returns>
        public ushort ToUInt16()
        {
            return (ushort)Value;
        }

        /// <summary>
        /// Converts this instance to a signed 32-bit integer value.
        /// </summary>
        /// <returns>The signed 32-bit integer representation of this result's value.</returns>
        public int ToInt32()
        {
            return (int)Value;
        }

        /// <summary>
        /// Converts this instance to an unsigned 32-bit integer value.
        /// </summary>
        /// <returns>The unsigned 32-bit integer representation of this result's value.</returns>
        public uint ToUInt32()
        {
            return Value;
        }

        /// <summary>
        /// Converts this instance to a signed 64-bit integer value.
        /// </summary>
        /// <returns>The signed 64-bit integer representation of this result's value.</returns>
        public long ToInt64()
        {
            return Value;
        }

        /// <summary>
        /// Converts this instance to an unsigned 64-bit integer value.
        /// </summary>
        /// <returns>The unsigned 64-bit integer representation of this result's value.</returns>
        public ulong ToUInt64()
        {
            return Value;
        }

        /// <summary>
        /// Determines whether this instance is equal to another <see cref="DialogBoxResult"/> instance.
        /// </summary>
        /// <param name="other">The other instance to compare with.</param>
        /// <returns><see langword="true"/> if both instances have the same name and value; otherwise, <see langword="false"/>.</returns>
        public bool Equals(DialogBoxResult other)
        {
            return Name == other.Name && Value == other.Value;
        }

        /// <summary>
        /// Determines whether this instance is equal to another object.
        /// </summary>
        /// <remarks>
        /// This method supports comparison with strings (case-insensitive) and numeric types,
        /// enabling PowerShell's -eq operator to work correctly with these types.
        /// </remarks>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><see langword="true"/> if the object is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj)
        {
            return obj switch
            {
                DialogBoxResult other => Equals(other),
                string s => string.Equals(Name, s, StringComparison.OrdinalIgnoreCase),
                sbyte n => Value == (uint)n,
                byte n => Value == n,
                short n => Value == (uint)n,
                ushort n => Value == n,
                int n => Value == (uint)n,
                uint n => Value == n,
                long n => Value == (ulong)n,
                ulong n => Value == n,
                _ => false
            };
        }

        /// <summary>
        /// Returns the name of this result.
        /// </summary>
        /// <returns>The name associated with this result instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hash code based on the value of this instance.</returns>
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// The name of this result for string comparisons.
        /// </summary>
        [DataMember]
        private readonly string Name;

        /// <summary>
        /// The numeric value representing this result.
        /// </summary>
        [DataMember]
        private readonly uint Value;

        /// <summary>
        /// Converts a DialogBoxResult instance to a signed byte value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator sbyte(DialogBoxResult result)
        {
            return result.ToSByte();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to an unsigned byte value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator byte(DialogBoxResult result)
        {
            return result.ToByte();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to a signed 16-bit integer value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator short(DialogBoxResult result)
        {
            return result.ToInt16();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to an unsigned 16-bit integer value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator ushort(DialogBoxResult result)
        {
            return result.ToUInt16();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to its underlying signed 32-bit integer value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator int(DialogBoxResult result)
        {
            return result.ToInt32();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to its underlying unsigned 32-bit integer value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator uint(DialogBoxResult result)
        {
            return result.ToUInt32();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to a signed 64-bit integer value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator long(DialogBoxResult result)
        {
            return result.ToInt64();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to an unsigned 64-bit integer value.
        /// </summary>
        /// <param name="result">The DialogBoxResult instance to convert.</param>
        public static explicit operator ulong(DialogBoxResult result)
        {
            return result.ToUInt64();
        }

        /// <summary>
        /// Converts a DialogBoxResult instance to its string representation.
        /// </summary>
        /// <remarks>This operator performs an explicit conversion, returning the name of the result.</remarks>
        /// <param name="result">The DialogBoxResult instance to convert to a string.</param>
        public static explicit operator string(DialogBoxResult result)
        {
            return result.ToString();
        }

        /// <summary>
        /// Determines whether two <see cref="DialogBoxResult"/> instances are equal.
        /// </summary>
        public static bool operator ==(DialogBoxResult left, DialogBoxResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="DialogBoxResult"/> instances are not equal.
        /// </summary>
        public static bool operator !=(DialogBoxResult left, DialogBoxResult right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Provides a read-only mapping between message box result values and their corresponding dialog box results.
        /// </summary>
        /// <remarks>This dictionary enables consistent translation of user responses from message boxes
        /// to dialog box results within the deployment session. The mapping is intended for internal use and is not
        /// typically accessed directly by consumers of the API.</remarks>
        private static readonly ReadOnlyDictionary<MESSAGEBOX_RESULT, DialogBoxResult> MessageBoxResultMap = new(typeof(DialogBoxResult).GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(static field => (MESSAGEBOX_RESULT)((DialogBoxResult)field.GetValue(null)!).Value, static field => (DialogBoxResult)field.GetValue(null)!));
    }
}
