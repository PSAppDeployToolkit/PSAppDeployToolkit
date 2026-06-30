/*
 * Copyright (C) 2025 Devicie Pty Ltd. All rights reserved.
 *
 * This file is part of PSAppDeployToolkit.
 *
 * PSAppDeployToolkit is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, either version 3
 * of the License, or (at your option) any later version.
 *
 * PSAppDeployToolkit is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with PSAppDeployToolkit. If not, see <https://www.gnu.org/licenses/>.
 */

using System;

namespace PSADT.SMBIOS
{
    /// <summary>
    /// The exception that is thrown when no SMBIOS structures of the specified type are found.
    /// </summary>
    /// <remarks>This exception is specifically used to indicate that a requested SMBIOS type does not exist
    /// in the current context.</remarks>
    /// <param name="Type">The SMBIOS type that was not found.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "The constructors here are correct for our specific, internal requirements.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "The constructors here are correct for our specific, internal requirements.")]
    internal sealed class SmbiosTypeNotFoundException(SmbiosType Type) : ArgumentOutOfRangeException($"No SMBIOS structures of type [{Type}] found.", (Exception?)null)
    {
        /// <summary>
        /// Represents the type of the System Management BIOS (SMBIOS) structure.
        /// </summary>
        /// <remarks>This field is used to identify the specific SMBIOS structure type. It is a read-only
        /// field and is intended for internal use within the system to categorize and process SMBIOS data.</remarks>
        internal readonly SmbiosType Type = Type;
    }
}
