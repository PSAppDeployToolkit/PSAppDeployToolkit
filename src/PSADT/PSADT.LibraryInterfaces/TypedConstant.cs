using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Windows.Win32.Foundation;

namespace PSADT.LibraryInterfaces
{
    /// <summary>
    /// Provides a base class for creating strongly-typed constant-like types with name and value semantics.
    /// </summary>
    /// <typeparam name="TSelf">The derived type implementing this pattern.</typeparam>
    /// <remarks>
    /// This abstract class provides common functionality for typed constant types including:
    /// <list type="bullet">
    /// <item>Name and value storage with equality semantics</item>
    /// <item>Conversion methods to various numeric types</item>
    /// <item>PowerShell-friendly equality comparison with strings and numbers</item>
    /// <item>Explicit cast operators for numeric and string conversions</item>
    /// </list>
    /// </remarks>
    [DataContract]
    public abstract class TypedConstant<TSelf> : IEquatable<TSelf> where TSelf : TypedConstant<TSelf>
    {
        /// <summary>
        /// The name of this constant value for string comparisons.
        /// </summary>
        [DataMember]
        private readonly string _name;

        /// <summary>
        /// The numeric value representing this constant.
        /// </summary>
        [DataMember]
        private readonly nint _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedConstant{TSelf}"/> class with the specified value.
        /// The name is automatically captured from the calling member.
        /// </summary>
        /// <param name="value">The numeric value to be associated with this instance.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private protected TypedConstant(nint value, [CallerMemberName] string name = null!)
        {
            _name = name;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedConstant{TSelf}"/> class with the specified <see cref="PCWSTR"/> value.
        /// The name is automatically captured from the calling member.
        /// </summary>
        /// <param name="value">The PCWSTR value to be associated with this instance.</param>
        /// <param name="name">The name of the constant, automatically captured from the calling member.</param>
        private protected TypedConstant(PCWSTR value, [CallerMemberName] string name = null!)
        {
            _name = name;
            unsafe
            {
                _value = (nint)value.Value;
            }
        }

        /// <summary>
        /// Converts this instance to a signed byte value.
        /// </summary>
        /// <returns>The signed byte representation of this constant's value.</returns>
        public sbyte ToSByte()
        {
            return (sbyte)_value;
        }

        /// <summary>
        /// Converts this instance to an unsigned byte value.
        /// </summary>
        /// <returns>The unsigned byte representation of this constant's value.</returns>
        public byte ToByte()
        {
            return (byte)_value;
        }

        /// <summary>
        /// Converts this instance to a signed 16-bit integer value.
        /// </summary>
        /// <returns>The signed 16-bit integer representation of this constant's value.</returns>
        public short ToInt16()
        {
            return (short)_value;
        }

        /// <summary>
        /// Converts this instance to an unsigned 16-bit integer value.
        /// </summary>
        /// <returns>The unsigned 16-bit integer representation of this constant's value.</returns>
        public ushort ToUInt16()
        {
            return (ushort)_value;
        }

        /// <summary>
        /// Converts this instance to a signed 32-bit integer value.
        /// </summary>
        /// <returns>The signed 32-bit integer representation of this constant's value.</returns>
        public int ToInt32()
        {
            return (int)_value;
        }

        /// <summary>
        /// Converts this instance to an unsigned 32-bit integer value.
        /// </summary>
        /// <returns>The unsigned 32-bit integer representation of this constant's value.</returns>
        public uint ToUInt32()
        {
            return (uint)_value;
        }

        /// <summary>
        /// Converts this instance to a signed 64-bit integer value.
        /// </summary>
        /// <returns>The signed 64-bit integer representation of this constant's value.</returns>
        public long ToInt64()
        {
            return _value;
        }

        /// <summary>
        /// Converts this instance to an unsigned 64-bit integer value.
        /// </summary>
        /// <returns>The unsigned 64-bit integer representation of this constant's value.</returns>
        public ulong ToUInt64()
        {
            return (ulong)_value;
        }

        /// <summary>
        /// Converts this instance to a native integer value.
        /// </summary>
        /// <returns>The native integer representation of this constant's value.</returns>
        public nint ToIntPtr()
        {
            return _value;
        }

        /// <summary>
        /// Converts this instance to a <see cref="PCWSTR"/> value.
        /// </summary>
        /// <returns>The PCWSTR representation of this constant's value.</returns>
        internal PCWSTR ToPCWSTR()
        {
            unsafe
            {
                return (PCWSTR)(void*)_value;
            }
        }

        /// <summary>
        /// Determines whether this instance is equal to another instance of the same type.
        /// </summary>
        /// <param name="other">The other instance to compare with.</param>
        /// <returns><see langword="true"/> if both instances have the same name and value; otherwise, <see langword="false"/>.</returns>
        public bool Equals(TSelf? other)
        {
            return other is not null && _name == other._name && _value == other._value;
        }

        /// <summary>
        /// Determines whether the specified object is equal to this instance.
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
                TSelf other => Equals(other),
                string s => string.Equals(_name, s, StringComparison.OrdinalIgnoreCase),
                sbyte n => _value == n,
                byte n => _value == n,
                short n => _value == n,
                ushort n => _value == n,
                int n => _value == n,
                uint n => _value == n,
                long n => _value == n,
                ulong n => _value == (nint)n,
                nint n => _value == n,
                nuint n => _value == (nint)n,
                _ => false
            };
        }

        /// <summary>
        /// Returns the name of this constant value.
        /// </summary>
        /// <returns>The name associated with this constant instance.</returns>
        public override string ToString()
        {
            return _name;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A hash code based on the value of this instance.</returns>
        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to a signed byte value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator sbyte(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToSByte();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to an unsigned byte value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator byte(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToByte();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to a signed 16-bit integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator short(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToInt16();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to an unsigned 16-bit integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator ushort(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToUInt16();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to a signed 32-bit integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator int(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToInt32();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to an unsigned 32-bit integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator uint(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToUInt32();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to a signed 64-bit integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator long(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToInt64();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to an unsigned 64-bit integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator ulong(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToUInt64();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to a native integer value.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator nint(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToIntPtr();
        }

        /// <summary>
        /// Converts a <see cref="TypedConstant{TSelf}"/> instance to its string representation.
        /// </summary>
        /// <param name="typedConstant">The typed constant instance to convert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="typedConstant"/> is null.</exception>
        public static explicit operator string(TypedConstant<TSelf> typedConstant)
        {
            return typedConstant is null
                ? throw new ArgumentNullException(nameof(typedConstant))
                : typedConstant.ToString();
        }
    }
}
