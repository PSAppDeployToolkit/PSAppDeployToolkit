using System;
using PSADT.DeviceManagement;
using PSADT.Interop;
using Xunit;

namespace PSADT.Tests.DeviceManagement
{
    /// <summary>
    /// Tests the description of what a machine is joined to.
    /// </summary>
    /// <remarks>
    /// The machine's own answer is checked where the query is tested; what is covered here is the shape
    /// the answer takes. The name being absent is meaningful rather than incidental - an unjoined machine
    /// has nothing to name - so a name that is present but blank has to be refused, or a caller cannot
    /// tell the two apart.
    /// </remarks>
    public sealed class DomainStatusTests
    {
        /// <summary>
        /// Verifies that the status and name handed in are the ones read back.
        /// </summary>
        [Fact]
        public void DomainStatus_KeepsWhatItIsGiven()
        {
            // Act
            DomainStatus status = new(NETSETUP_JOIN_STATUS.NetSetupDomainName, "CONTOSO");

            // Assert
            Assert.Equal(NETSETUP_JOIN_STATUS.NetSetupDomainName, status.JoinStatus);
            Assert.Equal("CONTOSO", status.DomainOrWorkgroupName);
        }

        /// <summary>
        /// Verifies that an unjoined machine is described with no name at all rather than an empty one.
        /// </summary>
        [Fact]
        public void DomainStatus_AllowsNoNameAtAll()
        {
            Assert.Null(new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupUnjoined, domainOrWorkgroupName: null).DomainOrWorkgroupName);
        }

        /// <summary>
        /// Verifies that a name that is present but blank is refused, since it would read as a machine
        /// joined to something unnamed.
        /// </summary>
        /// <param name="domainOrWorkgroupName">The blank name to refuse.</param>
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void DomainStatus_RefusesABlankName(string domainOrWorkgroupName)
        {
            _ = Assert.Throws<ArgumentException>(() => new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupDomainName, domainOrWorkgroupName));
        }

        /// <summary>
        /// Verifies that two descriptions of the same membership are equal.
        /// </summary>
        [Fact]
        public void Equality_IsByTheStatusAndTheName()
        {
            Assert.Equal(
                new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupDomainName, "CONTOSO"),
                new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupDomainName, "CONTOSO"));
            Assert.NotEqual(
                new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupDomainName, "CONTOSO"),
                new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupWorkgroupName, "CONTOSO"));
            Assert.NotEqual(
                new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupDomainName, "CONTOSO"),
                new DomainStatus(NETSETUP_JOIN_STATUS.NetSetupDomainName, "FABRIKAM"));
        }
    }
}
