using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using PSADT.Types;
using Windows.Win32;
using Windows.Win32.System.SystemServices;
using Windows.Win32.System.Diagnostics.Debug;

namespace PSADT.Shared
{
    /// <summary>
    /// A collection of utility methods for use in the PSADT module.
    /// </summary>
    public static class GeneralUtilities
    {
        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="img">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image img, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        /// <summary>
        /// Converts an image to an icon, automatically resizing to the maximum icon size if greater than 128px.
        /// </summary>
        /// <param name="img">The image to resize.</param>
        /// <returns>The resized image.</returns>
        public static Icon ConvertImageToIcon(Image img)
        {
            // Ensure the incoming image is < 128px in width/height.
            if ((img.Width > 128) || (img.Height > 128))
            {
                img = ResizeImage(img, 128, 128);
            }

            Icon icon;
            using (var msImg = new MemoryStream())
            using (var msIco = new MemoryStream())
            {
                img.Save(msImg, ImageFormat.Png);
                using (var bw = new BinaryWriter(msIco))
                {
                    bw.Write((short)0);           //0-1 reserved
                    bw.Write((short)1);           //2-3 image type, 1 = icon, 2 = cursor
                    bw.Write((short)1);           //4-5 number of images
                    bw.Write((byte)img.Width);    //6 image width
                    bw.Write((byte)img.Height);   //7 image height
                    bw.Write((byte)0);            //8 number of colors
                    bw.Write((byte)0);            //9 reserved
                    bw.Write((short)0);           //10-11 color planes
                    bw.Write((short)32);          //12-13 bits per pixel
                    bw.Write((int)msImg.Length);  //14-17 size of image data
                    bw.Write(22);                 //18-21 offset of image data
                    bw.Write(msImg.ToArray());    // write image data
                    bw.Flush();
                    bw.Seek(0, SeekOrigin.Begin);
                    icon = new Icon(msIco);
                }
            }
            return icon;
        }

        /// <summary>
        /// Converts a list of remaining arguments to a dictionary of key-value pairs.
        /// </summary>
        /// <param name="remainingArguments">A list of remaining arguments to convert.</param>
        /// <returns>A dictionary of key-value pairs representing the remaining arguments.</returns>
        public static Dictionary<string, object?> ConvertValuesFromRemainingArguments(List<object> remainingArguments)
        {
            Dictionary<string, object?> values = [];
            string currentKey = string.Empty;
            if ((null == remainingArguments) || (remainingArguments.Count == 0))
            {
                return values;
            }
            try
            {
                foreach (object argument in remainingArguments)
                {
                    if (null == argument)
                    {
                        continue;
                    }
                    if ((argument is string str) && Regex.IsMatch(str, "^-"))
                    {
                        currentKey = Regex.Replace(str, "(^-|:$)", string.Empty);
                        values.Add(currentKey, new SwitchParameter(true));
                    }
                    else if (!string.IsNullOrWhiteSpace(currentKey))
                    {
                        values[currentKey] = !string.IsNullOrWhiteSpace((string)((PSObject)ScriptBlock.Create("Out-String -InputObject $args[0]").InvokeReturnAsIs(argument)).BaseObject) ? argument : null;
                        currentKey = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FormatException("The parser was unable to process the provided arguments.", ex);
            }
            return values;
        }

        /// <summary>
        /// Converts a dictionary of key-value pairs to a string of PowerShell arguments.
        /// </summary>
        /// <param name="dict">A dictionary of key-value pairs to convert.</param>
        /// <param name="exclusions">An array of keys to exclude from the conversion.</param>
        /// <returns>A string of PowerShell arguments representing the dictionary.</returns>
        public static string ConvertDictToPowerShellArgs(IDictionary dict, string[]? exclusions = null)
        {
            List<string> args = [];
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key.ToString()!;
                string val = string.Empty;

                // Skip anything null or excluded.
                if (null == entry.Value)
                {
                    continue;
                }
                if ((null != exclusions) && exclusions.Contains(entry.Key.ToString()))
                {
                    continue;
                }

                // Handle nested dictionaries.
                if (entry.Value is IDictionary dictionary)
                {
                    args.Add(ConvertDictToPowerShellArgs(dictionary, exclusions));
                    continue;
                }

                // Handle all over values.
                if (entry.Value is string str)
                {
                    val = $"'{str.Replace("'", "''")}'";
                }
                else if (entry.Value is List<object> list)
                {
                    val = ConvertDictToPowerShellArgs(ConvertValuesFromRemainingArguments(list), exclusions);
                }
                else if (entry.Value is IEnumerable enumerable)
                {
                    if (enumerable.OfType<string>().ToArray() is string[] strings)
                    {
                        val = $"'{string.Join("','", strings.Select(s => s.Replace("'", "''")))}'";
                    }
                    else
                    {
                        val = string.Join(",", enumerable);
                    }
                }
                else if (entry.Value is not SwitchParameter)
                {
                    val = entry.Value.ToString()!;
                }

                // Add the key-value pair to the list.
                if (!string.IsNullOrWhiteSpace(val))
                {
                    args.Add($"-{key}:{val}");
                }
                else
                {
                    args.Add($"-{key}");
                }
            }
            return string.Join(" ", args);
        }

        /// <summary>
        /// Parses the specified PE file and returns information about it.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static ExecutableInfo GetExecutableInfo(string filePath)
        {
            bool HasCLRHeader(__IMAGE_DATA_DIRECTORY_16 dataDirectory)
            {
                if (dataDirectory.Length > 14)
                {
                    var comDir = dataDirectory._14;
                    return comDir.VirtualAddress != 0 && comDir.Size != 0;
                }
                return false;
            }

            T ReadStruct<T>(BinaryReader reader) where T : struct
            {
                var handle = GCHandle.Alloc(reader.ReadBytes(Marshal.SizeOf<T>()), GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
                }
                finally
                {
                    handle.Free();
                }
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            LibraryInterfaces.IMAGE_SUBSYSTEM subsystem = LibraryInterfaces.IMAGE_SUBSYSTEM.IMAGE_SUBSYSTEM_UNKNOWN;
            ExecutableType type = ExecutableType.Unknown;
            uint? entryPoint = null;
            ulong? imageBase = null;
            bool isDotNet = false;

            var dosHeader = ReadStruct<IMAGE_DOS_HEADER>(reader);
            if (dosHeader.e_magic != PInvoke.IMAGE_DOS_SIGNATURE)
            {
                throw new InvalidDataException("The specified file does not have a valid PE header.");
            }

            fs.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);
            if (reader.ReadUInt32() != PInvoke.IMAGE_NT_SIGNATURE)
            {
                throw new InvalidDataException("The specified file does not have a valid PE signature.");
            }

            var fileHeader = ReadStruct<IMAGE_FILE_HEADER>(reader);
            var magic = (IMAGE_OPTIONAL_HEADER_MAGIC)reader.ReadUInt16();
            fs.Seek(-2, SeekOrigin.Current);

            if (magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR32_MAGIC)
            {
                var opt32 = ReadStruct<IMAGE_OPTIONAL_HEADER32>(reader);
                subsystem = (LibraryInterfaces.IMAGE_SUBSYSTEM)opt32.Subsystem;
                entryPoint = opt32.AddressOfEntryPoint;
                imageBase = opt32.ImageBase;
                type = (ExecutableType)subsystem;
                isDotNet = HasCLRHeader(opt32.DataDirectory);
            }
            else if (magic == IMAGE_OPTIONAL_HEADER_MAGIC.IMAGE_NT_OPTIONAL_HDR64_MAGIC)
            {
                var opt64 = ReadStruct<IMAGE_OPTIONAL_HEADER64>(reader);
                subsystem = (LibraryInterfaces.IMAGE_SUBSYSTEM)opt64.Subsystem;
                entryPoint = opt64.AddressOfEntryPoint;
                imageBase = opt64.ImageBase;
                type = (ExecutableType)subsystem;
                isDotNet = HasCLRHeader(opt64.DataDirectory);
            }
            else
            {
                throw new InvalidDataException("The specified file does not have a valid optional header magic number.");
            }

            return new ExecutableInfo(
                filePath,
                (LibraryInterfaces.IMAGE_FILE_MACHINE)fileHeader.Machine,
                subsystem,
                (SystemArchitecture)fileHeader.Machine,
                type,
                isDotNet,
                entryPoint,
                imageBase
            );
        }
    }
}
