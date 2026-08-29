using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Xunit;

namespace PSADT.ClientServer.Server.Tests
{
    /// <summary>
    /// Tests the contract every payload crossing the pipe has to satisfy to survive the journey.
    /// </summary>
    /// <remarks>
    /// Each payload has its own tests, which round-trip an instance of that one type. What none of them can cover
    /// is the payload written next: <see cref="DataContractSerializer"/> does not complain about a member it was
    /// not told to carry, it simply leaves it out, so a new payload missing an attribute serializes cleanly, sends
    /// cleanly, and arrives with that member set to nothing at all.
    /// <para>
    /// So this sweeps the assembly rather than naming the types, and holds whatever it finds to the same rules.
    /// A payload added later is covered by it without anyone having to remember.
    /// </para>
    /// </remarks>
    public sealed class ClientServerPayloadContractTests
    {
        /// <summary>
        /// Verifies that every payload is declared as something the serializer will carry.
        /// </summary>
        [Fact]
        public void EveryPayload_IsDeclaredAsADataContract()
        {
            // Assert
            Assert.Equal(
                [.. Payloads.Select(static payload => $"{payload.Name} => contract")],
                [.. Payloads.Select(static payload => $"{payload.Name} => {(Attribute.IsDefined(payload, typeof(DataContractAttribute)) ? "contract" : "not a contract")}")]);
        }

        /// <summary>
        /// Verifies that every member a payload declares is said to be either carried or ignored.
        /// </summary>
        /// <remarks>
        /// The rule that matters, and the one nothing else enforces. A member marked neither way is dropped in
        /// silence, so the command arrives with a default in place of whatever the server meant to send - a
        /// timeout of zero, a dialog with no options, an environment variable with no name.
        /// <para>
        /// Being ignored counts as an answer. A payload holding its data in a backing field so that the record
        /// compares by value exposes a property that reads it, and that property must not be sent as well - so
        /// what is required is a decision either way rather than one particular attribute.
        /// </para>
        /// </remarks>
        [Fact]
        public void EveryPayloadMember_IsSaidToBeCarriedOrIgnored()
        {
            // Arrange
            List<string> undecided = [];

            // Act
            foreach (Type payload in Payloads)
            {
                foreach (MemberInfo member in SerializableMembers(payload))
                {
                    if (!Attribute.IsDefined(member, typeof(DataMemberAttribute)) && !Attribute.IsDefined(member, typeof(IgnoreDataMemberAttribute)))
                    {
                        undecided.Add($"{payload.Name}.{member.Name}");
                    }
                }
            }

            // Assert
            Assert.True(undecided.Count is 0, $"Neither carried nor ignored: {string.Join(", ", undecided)}.");
        }

        /// <summary>
        /// Verifies that every payload has something it actually sends.
        /// </summary>
        /// <remarks>
        /// A payload whose every member was ignored would satisfy the rule above while sending nothing at all.
        /// </remarks>
        [Fact]
        public void EveryPayload_CarriesAtLeastOneMember()
        {
            // Assert
            Assert.All(Payloads, static payload => Assert.Contains(
                SerializableMembers(payload),
                static member => Attribute.IsDefined(member, typeof(DataMemberAttribute))));
        }

        /// <summary>
        /// Verifies that every payload declares at least one member, and that the sweep found the payloads.
        /// </summary>
        /// <remarks>
        /// Two ways this file could stop testing anything without failing. The sweep could stop finding payloads,
        /// and a payload could declare nothing for the rule above to judge. Neither is a state the project is
        /// ever expected to be in, so both are asserted rather than assumed.
        /// </remarks>
        [Fact]
        public void PayloadsAndTheirMembers_AreFoundAtAll()
        {
            // Assert
            Assert.True(Payloads.Count >= 13, $"Only {Payloads.Count.ToString(CultureInfo.InvariantCulture)} payloads were found.");

            // Assert
            Assert.All(Payloads, static payload => Assert.NotEmpty(SerializableMembers(payload)));
        }

        /// <summary>
        /// The members of a payload that the serializer is expected to carry.
        /// </summary>
        /// <remarks>
        /// Anything the compiler wrote is left out. A record brings its own members - the equality contract, the
        /// backing fields of any auto-property - and none of them are the payload's own data.
        /// </remarks>
        /// <param name="payload">The payload to read.</param>
        /// <returns>The members the payload declares itself.</returns>
        private static IReadOnlyList<MemberInfo> SerializableMembers(Type payload)
        {
            return
            [
                .. payload
                    .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                    .Where(static member => member is FieldInfo or PropertyInfo && !Attribute.IsDefined(member, typeof(CompilerGeneratedAttribute))),
            ];
        }

        /// <summary>
        /// Every type that travels the pipe as a payload.
        /// </summary>
        private static readonly IReadOnlyList<Type> Payloads =
        [
            .. typeof(IClientServerPayload).Assembly.GetTypes()
                .Where(static type => type.IsClass && !type.IsAbstract && typeof(IClientServerPayload).IsAssignableFrom(type))
                .OrderBy(static type => type.Name, StringComparer.Ordinal),
        ];
    }
}
