// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iNKORE.UI.WPF.Modern.Gallery.DataModel;

namespace iNKORE.UI.WPF.Modern.Gallery.Helpers;

internal class IconsDataSource
{
    public static IconsDataSource Instance { get; } = new();

    public static List<IconData> Icons => Instance.icons;

    // Public list of available icon sets discovered via reflection
    public List<string> AvailableSets { get; } = new();

    // Current active set name (null = all)
    public string ActiveSet { get; private set; }

    private List<IconData> icons = new();

    private IconsDataSource() { }

    public object _lock = new();

    public async Task<List<IconData>> LoadIcons()
    {
        // Yield once to keep this method truly asynchronous without changing logic.
        await Task.Yield();
        // If already loaded, return current list
        lock (_lock)
        {
            if (icons.Count != 0)
            {
                return icons;
            }
        }

        // Try reflection-first: enumerate types in Common.IconKeys namespace
        try
        {
            var assembly = typeof(iNKORE.UI.WPF.Modern.Common.IconKeys.FontDictionary).Assembly;
            var types = assembly.GetTypes().Where(t => t.IsClass && t.IsSealed && t.IsAbstract && t.Namespace == "iNKORE.UI.WPF.Modern.Common.IconKeys");
            var discovered = new List<IconData>();

            foreach (var type in types)
            {
                // collect a set name for the class
                var setName = type.Name;
                // Try public static fields and properties of type FontIconData
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (var f in fields)
                {
                    if (f.FieldType.FullName == "iNKORE.UI.WPF.Modern.Common.IconKeys.FontIconData")
                    {
                        try
                        {
                            var value = f.GetValue(null);
                            var glyphProp = value?.GetType().GetProperty("Glyph");
                            var glyph = glyphProp?.GetValue(value) as string;
                            var familyProp = value?.GetType().GetProperty("FontFamily");
                            var family = familyProp?.GetValue(value) as System.Windows.Media.FontFamily;
                            var name = f.Name;
                            var data = new IconData { Name = name, Glyph = glyph, Set = setName, FontFamily = family };
                            discovered.Add(data);
                        }
                        catch { }
                    }
                }

                var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                foreach (var p in props)
                {
                    if (p.PropertyType.FullName == "iNKORE.UI.WPF.Modern.Common.IconKeys.FontIconData")
                    {
                        try
                        {
                            var value = p.GetValue(null);
                            var glyphProp = value?.GetType().GetProperty("Glyph");
                            var glyph = glyphProp?.GetValue(value) as string;
                            var familyProp = value?.GetType().GetProperty("FontFamily");
                            var family = familyProp?.GetValue(value) as System.Windows.Media.FontFamily;
                            var name = p.Name;
                            var data = new IconData { Name = name, Glyph = glyph, Set = setName, FontFamily = family };
                            discovered.Add(data);
                        }
                        catch { }
                    }
                }

                if (discovered.Any(d => d.Set == setName))
                {
                    AvailableSets.Add(setName);
                }
            }

            if (discovered.Count > 0)
            {
                lock (_lock)
                {
                    icons = discovered.OrderBy(i => i.Name).ToList();
                }
                // Ensure legacy/alias sets are present so the UI can show them
                EnsureLegacySets();
                return icons;
            }
        }
        catch
        {
            // reflection failed; no fallback
        }

        return icons;
    }

    //private static string ToCode(string glyph)
    //{
    //    if (string.IsNullOrEmpty(glyph)) return string.Empty;
    //    // glyph is a single-character string; convert to hex code (without leading 0x)
    //    var ch = glyph[0];
    //    return ((int)ch).ToString("X4");
    //}

    // Set active set and return filtered icons
    public List<IconData> SetActiveSet(string setName)
    {
        // Normalize legacy aliases to concrete set names when possible
        //if (string.Equals(setName, "SegoeMDL2Assets", StringComparison.OrdinalIgnoreCase) ||
        //    string.Equals(setName, "Segoe MDL2 Assets", StringComparison.OrdinalIgnoreCase))
        //{
        //    // These glyphs generally live in the JSON data under empty Set (or specific set names).
        //    // Treat this alias as a request to show all non-Fluent-only icons.
        //    ActiveSet = setName;
        //    return icons.Where(i => !i.IsSegoeFluentOnly).ToList();
        //}

        //if (string.Equals(setName, "SegoeIcons", StringComparison.OrdinalIgnoreCase) ||
        //    string.Equals(setName, "Segoe Icons", StringComparison.OrdinalIgnoreCase))
        //{
        //    // No dedicated SegoeIcons set in the built-in keys; treat as all icons (fallback).
        //    ActiveSet = setName;
        //    return icons;
        //}

        ActiveSet = setName;
        if (string.IsNullOrEmpty(setName)) return icons;
        return icons.Where(i => i.Set == setName).ToList();
    }

    private void EnsureLegacySets()
    {
    // No-op: legacy set aliases are handled in SetActiveSet().
    }
}
