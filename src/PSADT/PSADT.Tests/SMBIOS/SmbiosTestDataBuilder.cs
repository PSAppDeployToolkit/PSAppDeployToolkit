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
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using PSADT.SMBIOS;

namespace PSADT.Tests.SMBIOS
{
    internal static class SmbiosTestDataBuilder
    {
        internal static byte[] BuildRawSmbios(byte majorVersion, byte minorVersion, byte dmiRevision, IEnumerable<SmbiosStructure> structures)
        {
            List<byte> tableBytes = [];
            foreach (SmbiosStructure structure in structures)
            {
                byte structureLength = (byte)(4 + structure.FormattedData.Length);
                tableBytes.Add((byte)structure.Type);
                tableBytes.Add(structureLength);
                tableBytes.Add((byte)(structure.Handle & 0xFF));
                tableBytes.Add((byte)(structure.Handle >> 8));
                tableBytes.AddRange(structure.FormattedData);

                if (structure.Strings.Length == 0)
                {
                    tableBytes.Add(0);
                    tableBytes.Add(0);
                    continue;
                }

                foreach (string value in structure.Strings)
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        tableBytes.Add(0);
                        continue;
                    }

                    tableBytes.AddRange(Encoding.ASCII.GetBytes(value));
                    tableBytes.Add(0);
                }

                tableBytes.Add(0);
            }

            byte[] header = new byte[8];
            header[0] = 1;
            header[1] = majorVersion;
            header[2] = minorVersion;
            header[3] = dmiRevision;
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), (uint)tableBytes.Count);

            byte[] buffer = new byte[header.Length + tableBytes.Count];
            header.CopyTo(buffer, 0);
            tableBytes.CopyTo(buffer, header.Length);
            return buffer;
        }

        internal static byte[] BuildRawSmbios(params SmbiosStructure[] structures)
        {
            return BuildRawSmbios(3, 0, 0, structures);
        }

        internal readonly struct SmbiosStructure
        {
            internal SmbiosStructure(SmbiosType type, ushort handle, byte[] formattedData, params string[] strings)
            {
                Type = type;
                Handle = handle;
                FormattedData = formattedData ?? [];
                Strings = strings ?? [];
            }

            internal SmbiosType Type { get; }
            internal ushort Handle { get; }
            internal byte[] FormattedData { get; }
            internal string[] Strings { get; }
        }
    }
}
