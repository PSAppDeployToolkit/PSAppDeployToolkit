using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogResults
{
    /// <summary>
    /// Represents the possible outcomes of a dialog prompting the user to close applications.
    /// </summary>
    /// <remarks>This enumeration is used to indicate the user's response to a dialog that requests action regarding open applications, such as closing them, continuing without closing, or deferring the operation.</remarks>
    [DataContract]
    public readonly struct CloseAppsDialogResult : IDialogResult, IEquatable<CloseAppsDialogResult>
    {
        /// <summary>
        /// Returned when the user has not responded to the dialog in time.
        /// </summary>
        public static readonly CloseAppsDialogResult Timeout = Create(0);

        /// <summary>
        /// Specifies that the user has chosen to close the application.
        /// </summary>
        public static readonly CloseAppsDialogResult Close = Create(1);

        /// <summary>
        /// Specifies that the user has chosen to continue without closing the application.
        /// </summary>
        public static readonly CloseAppsDialogResult Continue = Create(2);

        /// <summary>
        /// Specifies that the user has chosen to defer the deployment.
        /// </summary>
        public static readonly CloseAppsDialogResult Defer = Create(3);

        /// <summary>
        /// Creates a new instance of the <see cref="CloseAppsDialogResult"/> struct.
        /// </summary>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        /// <param name="name">The name to be associated with this instance for string comparisons. Automatically captured from the caller member name.</param>
        /// <returns>A new <see cref="CloseAppsDialogResult"/> instance.</returns>
        private static CloseAppsDialogResult Create(uint value, [CallerMemberName] string name = "")
        {
            return new(name, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogResult"/> class with the specified value and name.
        /// </summary>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        /// <param name="name">The name to be associated with this instance for string comparisons.</param>
        private CloseAppsDialogResult(string name, uint value)
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
        /// Determines whether this instance is equal to another <see cref="CloseAppsDialogResult"/> instance.
        /// </summary>
        /// <param name="other">The other instance to compare with.</param>
        /// <returns><see langword="true"/> if both instances have the same name and value; otherwise, <see langword="false"/>.</returns>
        public bool Equals(CloseAppsDialogResult other)
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
                CloseAppsDialogResult other => Equals(other),
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
        /// Converts a CloseAppsDialogResult instance to a signed byte value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator sbyte(CloseAppsDialogResult result)
        {
            return result.ToSByte();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to an unsigned byte value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator byte(CloseAppsDialogResult result)
        {
            return result.ToByte();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to a signed 16-bit integer value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator short(CloseAppsDialogResult result)
        {
            return result.ToInt16();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to an unsigned 16-bit integer value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator ushort(CloseAppsDialogResult result)
        {
            return result.ToUInt16();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to its underlying signed 32-bit integer value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator int(CloseAppsDialogResult result)
        {
            return result.ToInt32();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to its underlying unsigned 32-bit integer value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator uint(CloseAppsDialogResult result)
        {
            return result.ToUInt32();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to a signed 64-bit integer value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator long(CloseAppsDialogResult result)
        {
            return result.ToInt64();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to an unsigned 64-bit integer value.
        /// </summary>
        /// <param name="result">The CloseAppsDialogResult instance to convert.</param>
        public static explicit operator ulong(CloseAppsDialogResult result)
        {
            return result.ToUInt64();
        }

        /// <summary>
        /// Converts a CloseAppsDialogResult instance to its string representation.
        /// </summary>
        /// <remarks>This operator performs an explicit conversion, returning the name of the result.</remarks>
        /// <param name="result">The CloseAppsDialogResult instance to convert to a string.</param>
        public static explicit operator string(CloseAppsDialogResult result)
        {
            return result.ToString();
        }

        /// <summary>
        /// Determines whether two <see cref="CloseAppsDialogResult"/> instances are equal.
        /// </summary>
        public static bool operator ==(CloseAppsDialogResult left, CloseAppsDialogResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="CloseAppsDialogResult"/> instances are not equal.
        /// </summary>
        public static bool operator !=(CloseAppsDialogResult left, CloseAppsDialogResult right)
        {
            return !left.Equals(right);
        }
    }
}
