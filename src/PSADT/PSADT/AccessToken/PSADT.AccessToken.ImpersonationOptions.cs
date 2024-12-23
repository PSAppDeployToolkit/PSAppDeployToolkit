using System;
using System.Collections.Generic;
using PSADT.PInvokes;

namespace PSADT.AccessToken
{
    /// <summary>
    /// Represents the options for impersonation.
    /// </summary>
    public sealed class ImpersonationOptions
    {
        private readonly List<TokenPrivilege> _privilegesToEnable = new();
        private readonly List<TokenPrivilege> _privilegesToDisable = new();

        /// <summary>
        /// Gets or sets a value indicating whether to reduce privileges when impersonating an administrator.
        /// </summary>
        public bool ReduceAdminPrivileges { get; set; } = true;

        /// <summary>
        /// Gets the list of privileges to enable when impersonating.
        /// </summary>
        public IReadOnlyList<TokenPrivilege> PrivilegesToEnable => _privilegesToEnable.AsReadOnly();

        /// <summary>
        /// Gets the list of privileges to disable when impersonating.
        /// </summary>
        public IReadOnlyList<TokenPrivilege> PrivilegesToDisable => _privilegesToDisable.AsReadOnly();

        /// <summary>
        /// Gets or sets a value indicating whether to allow impersonation of the SYSTEM account.
        /// </summary>
        internal bool AllowSystemImpersonation { get; private set; } = false;

        /// <summary>
        /// Adds a privilege to enable during impersonation.
        /// </summary>
        /// <param name="privilege">The privilege to enable.</param>
        public void AddPrivilegeToEnable(TokenPrivilege privilege)
        {
            if (!_privilegesToEnable.Contains(privilege))
            {
                _privilegesToEnable.Add(privilege);
            }
        }

        /// <summary>
        /// Adds a privilege to disable during impersonation.
        /// </summary>
        /// <param name="privilege">The privilege to disable.</param>
        public void AddPrivilegeToDisable(TokenPrivilege privilege)
        {
            if (!_privilegesToDisable.Contains(privilege))
            {
                _privilegesToDisable.Add(privilege);
            }
        }

        /// <summary>
        /// Adds multiple privileges to enable during impersonation.
        /// </summary>
        /// <param name="privileges">The privileges to enable.</param>
        public void AddPrivilegesToEnable(IEnumerable<TokenPrivilege> privileges)
        {
            if (privileges == null) throw new ArgumentNullException(nameof(privileges));
            foreach (var privilege in privileges)
            {
                AddPrivilegeToEnable(privilege);
            }
        }

        /// <summary>
        /// Adds multiple privileges to disable during impersonation.
        /// </summary>
        /// <param name="privileges">The privileges to disable.</param>
        public void AddPrivilegesToDisable(IEnumerable<TokenPrivilege> privileges)
        {
            if (privileges == null) throw new ArgumentNullException(nameof(privileges));
            foreach (var privilege in privileges)
            {
                AddPrivilegeToDisable(privilege);
            }
        }

        /// <summary>
        /// Removes a privilege from the enable list.
        /// </summary>
        /// <param name="privilege">The privilege to remove.</param>
        public void RemovePrivilegeToEnable(TokenPrivilege privilege)
        {
            _privilegesToEnable.Remove(privilege);
        }

        /// <summary>
        /// Removes a privilege from the disable list.
        /// </summary>
        /// <param name="privilege">The privilege to remove.</param>
        public void RemovePrivilegeToDisable(TokenPrivilege privilege)
        {
            _privilegesToDisable.Remove(privilege);
        }

        /// <summary>
        /// Clears all privileges from both enable and disable lists.
        /// </summary>
        public void ClearPrivileges()
        {
            _privilegesToEnable.Clear();
            _privilegesToDisable.Clear();
        }

        /// <summary>
        /// Creates a deep copy of the current ImpersonationOptions instance.
        /// </summary>
        /// <returns>A new ImpersonationOptions instance with copied values.</returns>
        public ImpersonationOptions Clone()
        {
            var clone = new ImpersonationOptions
            {
                ReduceAdminPrivileges = this.ReduceAdminPrivileges,
                AllowSystemImpersonation = this.AllowSystemImpersonation
            };

            clone.AddPrivilegesToEnable(_privilegesToEnable);
            clone.AddPrivilegesToDisable(_privilegesToDisable);

            return clone;
        }
    }
}
