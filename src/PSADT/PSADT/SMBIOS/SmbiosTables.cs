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
using PSADT.LibraryInterfaces;
using Windows.Win32.System.SystemInformation;

namespace PSADT.SMBIOS
{
    /// <summary>
    /// Provides methods for retrieving SMBIOS (System Management BIOS) firmware tables from the system.
    /// </summary>
    /// <remarks>The <see cref="SmbiosTables"/> class offers functionality to query and retrieve SMBIOS
    /// firmware tables,  which contain system hardware and firmware information. These methods rely on the underlying
    /// system  firmware table provider and identifiers to locate and access the SMBIOS data.</remarks>
    internal static class SmbiosTables
    {
        /// <summary>
        /// Retrieves the size, in bytes, of the system firmware table for the specified provider and table identifier.
        /// </summary>
        /// <remarks>This method queries the system firmware table without retrieving its contents. It is
        /// useful for determining the buffer size required to retrieve the table data. The method relies on the
        /// specified firmware table provider and table identifier to locate the appropriate table.</remarks>
        /// <returns>The size of the system firmware table, in bytes.</returns>
        internal static int GetRequiredLength()
        {
            uint size = Kernel32.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, FIRMWARE_TABLE_ID.SMBIOS, null);
            return size <= int.MaxValue ? (int)size : throw new InvalidOperationException("SMBIOS table size exceeds supported limits.");
        }

        /// <summary>
        /// Retrieves the SMBIOS firmware table and writes it to the specified buffer.
        /// </summary>
        /// <remarks>This method uses the underlying system firmware table provider to retrieve the SMBIOS
        /// data. Ensure that the buffer is properly sized to avoid data truncation.</remarks>
        /// <param name="buffer">A span of bytes where the SMBIOS firmware table will be written. The buffer must be large enough to hold the
        /// table data.</param>
        internal static void FillBuffer(Span<byte> buffer)
        {
            uint written = Kernel32.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, FIRMWARE_TABLE_ID.SMBIOS, buffer);
            if (written != (uint)buffer.Length)
            {
                throw new InvalidOperationException($"Unexpected SMBIOS byte count. Expected {buffer.Length}, wrote {written}.");
            }
        }
    }
}
